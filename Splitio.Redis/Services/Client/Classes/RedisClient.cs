using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Common;
using Splitio.Redis.Services.Domain;
using Splitio.Redis.Services.Events.Classes;
using Splitio.Redis.Services.Impressions.Classes;
using Splitio.Redis.Services.Parsing.Classes;
using Splitio.Redis.Services.Shared;
using Splitio.Redis.Telemetry.Storages;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Client.Classes;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Evaluator;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.Shared.Classes;

namespace Splitio.Redis.Services.Client.Classes
{
    public class RedisClient : SplitClient
    {
        private readonly RedisConfig _config;

        private IRedisAdapter _redisAdapter;
        private IImpressionsCache _impressionsCache;
        private IConnectionPoolManager _connectionPoolManager;
        private IFeatureFlagCacheConsumer _featureFlagCacheConsumer;
        private ISegmentCacheConsumer _segmentCacheConsumer;

        public RedisClient(ConfigurationOptions config, string apiKey) : base(apiKey)
        {
            _config = new RedisConfig();

            ReadConfig(config);
            BuildRedisCache();
            BuildTreatmentLog(config.ImpressionListener);

            BuildSenderAdapter();
            BuildUniqueKeysTracker(_config);
            BuildImpressionsCounter(_config);
            BuildImpressionsObserver();
            BuildImpressionManager();

            BuildEventLog();
            BuildManager();
            BuildEvaluator();
            BuildSyncManager();

            BuildClientExtension();

            _syncManager.Start();
        }

        #region Private Methods
        private void ReadConfig(ConfigurationOptions config)
        {            
            var baseConfig = _configService.ReadConfig(config, ConfingTypes.Redis);
            _config.SdkVersion = baseConfig.SdkVersion;
            _config.SdkMachineName = baseConfig.SdkMachineName;
            _config.SdkMachineIP = baseConfig.SdkMachineIP;
            _config.BfExpectedElements = baseConfig.BfExpectedElements;
            _config.BfErrorRate = baseConfig.BfErrorRate;
            _config.UniqueKeysRefreshRate = baseConfig.UniqueKeysRefreshRate;
            _config.UniqueKeysCacheMaxSize = baseConfig.UniqueKeysCacheMaxSize;
            _config.UniqueKeysBulkSize = baseConfig.UniqueKeysBulkSize;
            _config.ImpressionsMode = baseConfig.ImpressionsMode;
            _config.ImpressionsCounterRefreshRate = baseConfig.ImpressionsCounterRefreshRate;
            _config.ImpressionsCounterCacheMaxSize = baseConfig.ImpressionsCounterCacheMaxSize;
            _config.ImpressionsCountBulkSize = baseConfig.ImpressionsCountBulkSize;
            _config.LabelsEnabled = baseConfig.LabelsEnabled;
            _config.FlagSetsFilter = baseConfig.FlagSetsFilter;
            _config.Mode = config.Mode;
            _config.FromCacheAdapterConfig(config.CacheAdapterConfig);
    }

        private void BuildRedisCache()
        {
            _connectionPoolManager = new ConnectionPoolManager(_config);
            _redisAdapter = new RedisAdapter(_config, _connectionPoolManager);
            BuildTelemetryStorage();

            _segmentCacheConsumer = new RedisSegmentCache(_redisAdapter, _config.RedisUserPrefix);
            _splitParser = new RedisSplitParser(_segmentCacheConsumer);
            _featureFlagCacheConsumer = new RedisSplitCache(_redisAdapter, _splitParser, _config.RedisUserPrefix);
            _blockUntilReadyService = new RedisBlockUntilReadyService(_redisAdapter);
            _trafficTypeValidator = new TrafficTypeValidator(_featureFlagCacheConsumer, _blockUntilReadyService);
        }

        private void BuildTreatmentLog(IImpressionListener impressionListener)
        {
            _impressionsCache = new RedisImpressionsCache(_redisAdapter, _config.SdkMachineIP, _config.SdkVersion, _config.SdkMachineName, _config.RedisUserPrefix);
            _impressionsLog = new RedisImpressionLog(_impressionsCache);
            _customerImpressionListener = impressionListener;
        }

        private void BuildSenderAdapter()
        {
            _impressionsSenderAdapter = new RedisSenderAdapter(_impressionsCache);
        }

        private void BuildImpressionsObserver()
        {
            if (_config.ImpressionsMode != ImpressionsMode.Optimized)
            {
                _impressionsObserver = new NoopImpressionsObserver();
                return;
            }

            var impressionHasher = new ImpressionHasher();
            _impressionsObserver = new ImpressionsObserver(impressionHasher);
        }

        private void BuildImpressionManager()
        {
            var shouldCalculatePreviousTime = _config.ImpressionsMode == ImpressionsMode.Optimized;

            _impressionsManager = new ImpressionsManager(_impressionsLog, _customerImpressionListener, _impressionsCounter, shouldCalculatePreviousTime, _config.ImpressionsMode, null, _tasksManager, _uniqueKeysTracker, _impressionsObserver, _config.LabelsEnabled);
        }

        private void BuildEventLog()
        {
            var eventsCache = new RedisEventsCache(_redisAdapter, _config.SdkMachineName, _config.SdkMachineIP, _config.SdkVersion, _config.RedisUserPrefix);
            _eventsLog = new RedisEvenstLog(eventsCache, _tasksManager);
        }
        
        private void BuildManager()
        {
            _manager = new SplitManager(_featureFlagCacheConsumer, _blockUntilReadyService);
        }

        private void BuildEvaluator()
        {
            var splitter = new Splitter();
            _evaluator = new Evaluator(_featureFlagCacheConsumer, splitter, _telemetryEvaluationProducer);
        }

        private void BuildTelemetryStorage()
        {
            var redisTelemetryStorage = new RedisTelemetryStorage(_redisAdapter, _config.RedisUserPrefix, _config.SdkVersion, _config.SdkMachineIP, _config.SdkMachineName);

            _telemetryInitProducer = redisTelemetryStorage;
            _telemetryEvaluationProducer = redisTelemetryStorage;
        }

        private void BuildSyncManager()
        {
            _syncManager = new RedisSyncManager(_uniqueKeysTracker, _impressionsCounter, _connectionPoolManager, _tasksManager, _telemetryInitProducer, _factoryInstantiationsService);
        }
        #endregion
    }
}
