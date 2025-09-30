using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Common;
using Splitio.Redis.Services.Domain;
using Splitio.Redis.Services.Events.Classes;
using Splitio.Redis.Services.Impressions.Classes;
using Splitio.Redis.Services.Shared;
using Splitio.Redis.Telemetry.Storages;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Client.Classes;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Evaluator;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.Parsing;
using Splitio.Services.Shared.Classes;

namespace Splitio.Redis.Services.Client.Classes
{
    public class RedisClient : SplitClient
    {
        private readonly RedisConfig _config;
        private IRedisAdapterConsumer _redisAdapterConsumer;
        private IRedisAdapterProducer _redisAdapterProducer;
        
        private IImpressionsCache _impressionsCache;
        private ConnectionPoolManager _connectionPoolManager;
        private IFeatureFlagCacheConsumer _featureFlagCacheConsumer;
        private readonly new FallbackTreatmentCalculator _fallbackTreatmentCalculator;

        public RedisClient(ConfigurationOptions config, string apiKey, FallbackTreatmentCalculator fallbackTreatmentCalculator) : base(apiKey, fallbackTreatmentCalculator)
        {
            _config = new RedisConfig();
            _fallbackTreatmentCalculator = fallbackTreatmentCalculator;

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
            var baseConfig = _configService.ReadConfig(config, ConfigTypes.Redis);
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
            _config.FlagSetsInvalid = baseConfig.FlagSetsInvalid;
            _config.Mode = config.Mode;
            _config.FromCacheAdapterConfig(config.CacheAdapterConfig);
        }

        private void BuildRedisCache()
        {
            _connectionPoolManager = new ConnectionPoolManager(_config);
            _redisAdapterConsumer = new RedisAdapterConsumer(_config, _connectionPoolManager);
            _redisAdapterProducer = new RedisAdapterProducer(_config, _connectionPoolManager);

            BuildTelemetryStorage();

            var segmentCacheConsumer = new RedisSegmentCache(_redisAdapterConsumer, _config, _connectionPoolManager.IsClusterMode());
            var rbsParser = new RuleBasedSegmentParser(segmentCacheConsumer, null);
            var ruleBasedSegmentCacheConsumer = new RedisRuleBasedSegmentCache(_redisAdapterConsumer, rbsParser, _config, _connectionPoolManager.IsClusterMode());
            _splitParser = new FeatureFlagParser(segmentCacheConsumer, null);
            _featureFlagCacheConsumer = new RedisSplitCache(_redisAdapterConsumer, _splitParser, _config, _connectionPoolManager.IsClusterMode(), ruleBasedSegmentCacheConsumer);
            _blockUntilReadyService = new RedisBlockUntilReadyService(_redisAdapterConsumer);
            _trafficTypeValidator = new TrafficTypeValidator(_featureFlagCacheConsumer, _blockUntilReadyService);
        }

        private void BuildTreatmentLog(IImpressionListener impressionListener)
        {
            _impressionsCache = new RedisImpressionsCache(_redisAdapterProducer, _config, _connectionPoolManager.IsClusterMode());
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

            _impressionsManager = new ImpressionsManager(_impressionsLog, _customerImpressionListener, _impressionsCounter, shouldCalculatePreviousTime, _config.ImpressionsMode, null, _tasksManager, _uniqueKeysTracker, _impressionsObserver, _config.LabelsEnabled, _propertiesValidator);
        }

        private void BuildEventLog()
        {
            var eventsCache = new RedisEventsCache(_redisAdapterProducer, _config, _connectionPoolManager.IsClusterMode());
            _eventsLog = new RedisEvenstLog(eventsCache, _tasksManager);
        }
        
        private void BuildManager()
        {
            _manager = new SplitManager(_featureFlagCacheConsumer, _blockUntilReadyService);
        }

        private void BuildEvaluator()
        {
            var splitter = new Splitter();
            _evaluator = new Evaluator(_featureFlagCacheConsumer, splitter, _telemetryEvaluationProducer, _fallbackTreatmentCalculator);
        }

        private void BuildTelemetryStorage()
        {
            var redisTelemetryStorage = new RedisTelemetryStorage(_redisAdapterProducer, _config, _connectionPoolManager.IsClusterMode());

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