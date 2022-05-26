using Splitio.Services.Shared.Classes;
using System.Threading;

namespace Splitio.Services.Impressions.Classes
{
    public abstract class TrackerComponent
    {
        protected readonly ITasksManager _tasksManager;
        protected readonly int _taskInterval;
        protected readonly int _cacheMaxSize;
        protected readonly int _maxBulkSize;

        protected readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        protected readonly object _taskLock = new object();
        protected readonly object _lock = new object();

        protected bool _running = false;

        public TrackerComponent(ComponentConfig config,
            ITasksManager tasksManager)
        {
            _taskInterval = config.PeriodicTaskIntervalSeconds;
            _cacheMaxSize = config.CacheMaxSize;
            _maxBulkSize = config.MaxBulkSize;

            _tasksManager = tasksManager;
        }
        
        public void Start()
        {
            lock (_taskLock)
            {
                if (_running) return;

                _running = true;
                StartTask();
            }
        }

        public void Stop()
        {
            lock (_taskLock)
            {
                if (!_running) return;

                _running = false;
                _cancellationTokenSource.Cancel();
                SendBulkData();
            }
        }

        protected abstract void StartTask();
        protected abstract void SendBulkData();
    }

    public class ComponentConfig
    {
        public int PeriodicTaskIntervalSeconds { get; set; }
        public int CacheMaxSize { get; set; }
        public int MaxBulkSize { get; set; }

        public ComponentConfig(int periodicTaskIntervalSeconds, int cacheMaxSize, int maxBulkSize)
        {
            PeriodicTaskIntervalSeconds = periodicTaskIntervalSeconds;
            CacheMaxSize = cacheMaxSize;
            MaxBulkSize = maxBulkSize;
        }
    }
}
