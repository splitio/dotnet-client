using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Common
{
    internal class RedisSyncManager : ISyncManager
    {
        private readonly IUniqueKeysTracker _uniqueKeysTracker;
        private readonly IImpressionsCounter _impressionsCounter;
        private readonly IConnectionPoolManager _connectionPoolManager;
        private readonly ITasksManager _tasksManager;
        private readonly ITelemetryInitProducer _telemetryInitProducer;
        private readonly IFactoryInstantiationsService _factoryInstantiationsService;

        public RedisSyncManager(IUniqueKeysTracker uniqueKeysTracker,
            IImpressionsCounter impressionsCounter,
            IConnectionPoolManager connectionPoolManager,
            ITasksManager tasksManager,
            ITelemetryInitProducer telemetryInitProducer,
            IFactoryInstantiationsService factoryInstantiationsService)
        {
            _uniqueKeysTracker = uniqueKeysTracker;
            _impressionsCounter = impressionsCounter;
            _connectionPoolManager = connectionPoolManager;
            _tasksManager = tasksManager;
            _telemetryInitProducer = telemetryInitProducer;
            _factoryInstantiationsService = factoryInstantiationsService;
        }

        public void Start()
        {
            _uniqueKeysTracker.Start();
            _impressionsCounter.Start();
            _tasksManager.NewOnTimeTaskAndStart(Enums.Task.TelemetryInit, RecordConfigInitAsync);
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

        private async Task RecordConfigInitAsync()
        {
            var config = new Config
            {
                OperationMode = (int)Mode.Consumer,
                Storage = Constants.StorageType.Redis,
                ActiveFactories = _factoryInstantiationsService.GetActiveFactories(),
                RedundantActiveFactories = _factoryInstantiationsService.GetRedundantActiveFactories()
            };

            await _telemetryInitProducer.RecordConfigInitAsync(config);
        }
    }
}
