using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Localhost
{
    public class FileSyncWatcher : ILocalhostFileSync
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger("SplitFileSystemWatcherLocal");

        private readonly FileSystemWatcher _watcher;
        private readonly ISplitTask _task;
        private readonly CancellationTokenSource _cts;
        private readonly BlockingCollection<FileSystemEventArgs> _queue;
        
        private Action _onFileChanged;

        public FileSyncWatcher(string path,
            ISplitTask task)
        {
            var directoryPath = Path.GetDirectoryName(path);
            _watcher = new FileSystemWatcher(string.IsNullOrEmpty(directoryPath) ? Directory.GetCurrentDirectory() : directoryPath, Path.GetFileName(path))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _task = task;
            _task.SetAction(Work);
            _cts = new CancellationTokenSource();
            _queue = new BlockingCollection<FileSystemEventArgs>(new ConcurrentQueue<FileSystemEventArgs>());
        }

        public void SetOnFileChangedAction(Action onFileChanged)
        {
            _onFileChanged = onFileChanged;
        }

        public void Start(string filePath)
        {
            _watcher.Changed += (sender, e) => { _queue.Add(e); };
            _task.Start();
        }

        public async Task StopAsync()
        {
            try
            {
                _cts.Cancel();
                await _task.StopAsync();
                _cts?.Dispose();
            }
            catch (Exception ex)
            {
                _log.Debug("Somenting went wrong stopping SplitFileWatcher.", ex);
            }
        }

        private void Work()
        {
            try
            {
                if (_queue.TryTake(out var _, -1, _cts.Token))
                {
                    _onFileChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException) return;

                _log.Warn("Somenting went wrong processing SplitFileUpdate.", ex);
            }
        }
    }
}
