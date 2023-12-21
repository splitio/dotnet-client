using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Localhost
{
    public class FileSyncPolling : ILocalhostFileSync
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger("FileUpdateWatcher");

        private readonly int _intervalMs;
        private readonly CancellationTokenSource _cts;

        private string _path;
        private Action _onFileChanged;
        private SplitPeriodicTask _task;
        private DateTime? _fileLastUpdatedDateTime;
        private IStatusManager _statusManager;

        public FileSyncPolling(int intervalMs)
        {
            _intervalMs = intervalMs;
            _cts = new CancellationTokenSource();
        }

        public void SetStatusManager(IStatusManager statusManager)
        {
            _statusManager = statusManager;
        }

        public void SetOnFileChangedAction(Action onFileChanged)
        {
            _onFileChanged = onFileChanged;
        }

        public void Start(string filePath)
        {
            if (_task == null)
            {
                _task = new SplitPeriodicTask(_statusManager, Enums.Task.LocalhostFileSync, _intervalMs);
            }

            _path = filePath;
            _task.SetAction(Work);
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
                if (string.IsNullOrEmpty(_path))
                {
                    _log.Debug($"File path must not be empty.");
                    return;
                }

                if (!File.Exists(_path))
                {
                    _log.Debug($"File {_path} does not exist.");
                    return;
                }

                var lastUpdated = File.GetLastWriteTime(_path);

                if (_fileLastUpdatedDateTime == lastUpdated)
                {
                    _log.Debug($"File {_path} was last updated at {_fileLastUpdatedDateTime}");
                    return;
                }

                _fileLastUpdatedDateTime = lastUpdated;

                _onFileChanged?.Invoke();
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException) return;

                _log.Warn("Somenting went wrong processing SplitFileUpdate.", ex);
            }
        }
    }
}
