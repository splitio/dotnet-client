using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Impressions.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Common
{
    internal class RedisSyncManager : ISyncManager
    {
        private readonly IUniqueKeysTracker _uniqueKeysTracker;
        private readonly IImpressionsCounter _impressionsCounter;
        private readonly IConnectionPoolManager _connectionPoolManager;

        public RedisSyncManager(IUniqueKeysTracker uniqueKeysTracker,
            IImpressionsCounter impressionsCounter,
            IConnectionPoolManager connectionPoolManager)
        {
            _uniqueKeysTracker = uniqueKeysTracker;
            _impressionsCounter = impressionsCounter;
            _connectionPoolManager = connectionPoolManager;
        }

        public void Start()
        {
            _uniqueKeysTracker.Start();
            _impressionsCounter.Start();
        }

        public void Shutdown()
        {
            var task = new List<Task>
            {
                _uniqueKeysTracker.StopAsync(),
                _impressionsCounter.StopAsync()
            };

            Task.WaitAll(task.ToArray(), Constants.Gral.DestroyTimeount);
            _connectionPoolManager.Dispose();
        }

        public async Task ShutdownAsync()
        {
            await _uniqueKeysTracker.StopAsync();
            await _impressionsCounter.StopAsync();
            _connectionPoolManager.Dispose();
        }
    }
}
