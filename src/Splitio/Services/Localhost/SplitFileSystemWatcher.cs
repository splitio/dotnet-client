using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Splitio.Services.Localhost
{
    public class SplitFileSystemWatcher : BaseSplitFileWatcher
    {
        private readonly FileSystemWatcher _watcher;
        private readonly BlockingCollection<FileSystemEventArgs> _queue;

        public SplitFileSystemWatcher(ILocalhostFileService localhostFileService,
            IFeatureFlagCache featureFlagCache,
            ISplitTask worker,
            string fullPath) : base(localhostFileService, featureFlagCache, worker, fullPath)
        {
            _queue = new BlockingCollection<FileSystemEventArgs>(new ConcurrentQueue<FileSystemEventArgs>());
            var directoryPath = Path.GetDirectoryName(_fullPath);
            _watcher = new FileSystemWatcher(string.IsNullOrEmpty(directoryPath) ? Directory.GetCurrentDirectory() : directoryPath, Path.GetFileName(_fullPath))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _watcher.Changed += (sender, e) => { _queue.Add(e); };
        }

        protected override void Work()
        {
            try
            {
                if (_queue.TryTake(out var _, -1, _cts.Token))
                {
                    ProcessSplitFileUpdate();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"#1: {ex.ToString()} ");
            }
        }
    }
}
