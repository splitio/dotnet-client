using System;
using System.IO;
using System.Threading.Tasks;

namespace Splitio.Services.Localhost
{
    public class FileSyncWatcher : ILocalhostFileSync
    {
        private readonly FileSystemWatcher _watcher;        
        private Action _onFileChanged;

        public FileSyncWatcher(string path)
        {
            var directoryPath = Path.GetDirectoryName(path);
            _watcher = new FileSystemWatcher(string.IsNullOrEmpty(directoryPath) ? Directory.GetCurrentDirectory() : directoryPath, Path.GetFileName(path))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
        }

        public void SetOnFileChangedAction(Action onFileChanged)
        {
            _onFileChanged = onFileChanged;
        }

        public void Start(string filePath)
        {
            _watcher.Changed += (sender, e) => { _onFileChanged?.Invoke(); };
        }

        public Task StopAsync()
        {
            _watcher.Dispose();
            return Task.FromResult(0);
        }
    }
}
