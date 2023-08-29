using Splitio.Services.Tasks;

namespace Splitio.Services.Impressions.Classes
{
    public abstract class TrackerComponent
    {
        protected readonly ISplitTask _task;
        protected readonly int _cacheMaxSize;
        protected readonly int _maxBulkSize;

        protected readonly object _lock = new object();

        public TrackerComponent(ComponentConfig config,
            ISplitTask task)
        {
            _cacheMaxSize = config.CacheMaxSize;
            _maxBulkSize = config.MaxBulkSize;

            _task = task;
            _task.SetAction(SendBulkData);
        }
        
        public void Start()
        {
            if (_task.IsRunning()) return;

            StartTask();
        }

        public void Stop()
        {
            if (!_task.IsRunning()) return;
                
            StopTask();
            SendBulkData();
        }

        protected virtual void StartTask()
        {
            _task.Start();
        }

        protected virtual void StopTask()
        {
            _task.Stop();
        }

        protected abstract void SendBulkData();
    }

    public class ComponentConfig
    {
        public int CacheMaxSize { get; set; }
        public int MaxBulkSize { get; set; }

        public ComponentConfig(int cacheMaxSize, int maxBulkSize)
        {
            CacheMaxSize = cacheMaxSize;
            MaxBulkSize = maxBulkSize;
        }
    }
}
