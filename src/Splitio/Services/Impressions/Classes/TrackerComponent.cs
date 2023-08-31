using Splitio.Services.Tasks;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Classes
{
    public abstract class TrackerComponent
    {
        protected readonly ISplitTask _task;
        protected readonly int _cacheMaxSize;
        protected readonly int _maxBulkSize;

        public TrackerComponent(ComponentConfig config,
            ISplitTask task)
        {
            _cacheMaxSize = config.CacheMaxSize;
            _maxBulkSize = config.MaxBulkSize;

            _task = task;
            _task.SetFunction(SendBulkDataAsync);
            _task.OnStop(SendBulkDataAsync);
        }
        
        public void Start()
        {
            StartTask();
        }

        public async Task StopAsync()
        {
            await StopTaskAsync();
        }

        protected virtual void StartTask()
        {
            _task.Start();
        }

        protected virtual async Task StopTaskAsync()
        {
            await _task.StopAsync();
        }

        protected abstract Task SendBulkDataAsync();
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
