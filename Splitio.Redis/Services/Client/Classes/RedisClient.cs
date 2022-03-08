using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Redis.Services.Events.Classes;
using Splitio.Redis.Services.Impressions.Classes;
using Splitio.Redis.Services.Parsing.Classes;
using Splitio.Redis.Services.Shared;
using Splitio.Redis.Telemetry.Storages;
using Splitio.Services.Cache.Filter;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Client.Classes
{
    public class RedisClient : SplitClient
    {
        private readonly RedisConfig _config;

        private IRedisAdapter _redisAdapter;
        private IImpressionsCountSender _impressionCounterSender;
        private IImpressionsCache _impressionsCache;

        public RedisClient(ConfigurationOptions config,
            string apiKey,
            ISplitLogger log = null) : base(GetLogger(log))
        {
            _config = new RedisConfig();
            ApiKey = apiKey;

            ReadConfig(config);
            BuildRedisCache();
            BuildTelemetryStorage();
            BuildTreatmentLog(config);
            BuildUniqueKeysTracker();
            BuildImpressionManager();
            BuildEventLog();
            BuildBlockUntilReadyService();
            BuildManager();
            BuildEvaluator();

            Start();
        }

        public override void Destroy()
        {
            if (_statusManager.IsDestroyed()) return;

            _uniqueKeysTracker.Stop();
            _impressionCounterSender.Stop();
            base.Destroy();
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
            _config.ImpressionsMode = baseConfig.ImpressionsMode;
            LabelsEnabled = baseConfig.LabelsEnabled;

            _config.RedisHost = config.CacheAdapterConfig.Host;
            _config.RedisPort = config.CacheAdapterConfig.Port;
            _config.RedisPassword = config.CacheAdapterConfig.Password;
            _config.RedisDatabase = config.CacheAdapterConfig.Database ?? 0;
            _config.RedisConnectTimeout = config.CacheAdapterConfig.ConnectTimeout ?? 0;
            _config.RedisSyncTimeout = config.CacheAdapterConfig.SyncTimeout ?? 0;
            _config.RedisConnectRetry = config.CacheAdapterConfig.ConnectRetry ?? 0;
            _config.RedisUserPrefix = config.CacheAdapterConfig.UserPrefix;
            _config.Mode = config.Mode;
            _config.TlsConfig = config.CacheAdapterConfig.TlsConfig;
        }

        private void BuildRedisCache()
        {
            _redisAdapter = new RedisAdapter(_config.RedisHost, _config.RedisPort, _config.RedisPassword, _config.RedisDatabase, _config.RedisConnectTimeout, _config.RedisConnectRetry, _config.RedisSyncTimeout, _config.TlsConfig);

            Task.Factory.StartNew(() =>
            {
                _redisAdapter.Connect();
                RecordConfigInit();
            });

            _segmentCache = new RedisSegmentCache(_redisAdapter, _config.RedisUserPrefix);
            _splitParser = new RedisSplitParser(_segmentCache);
            _splitCache = new RedisSplitCache(_redisAdapter, _splitParser, _config.RedisUserPrefix);            
            _trafficTypeValidator = new TrafficTypeValidator(_splitCache);
        }

        private void BuildTreatmentLog(ConfigurationOptions config)
        {
            _impressionsCache = new RedisImpressionsCache(_redisAdapter, _config.SdkMachineIP, _config.SdkVersion, _config.SdkMachineName, _config.RedisUserPrefix);
            _impressionsLog = new RedisImpressionLog(_impressionsCache);
            _customerImpressionListener = config.ImpressionListener;
        }

        private void BuildUniqueKeysTracker()
        {
            var bloomFilter = new BloomFilter(_config.BfExpectedElements, _config.BfErrorRate);
            var adapter = new FilterAdapter(bloomFilter);
            var trackerCache = new ConcurrentDictionary<string, HashSet<string>>();

            var senderAdapter = new RedisSenderAdapter(_impressionsCache);
            var config = new TrackerConfig
            {
                CacheMaxSize = _config.UniqueKeysCacheMaxSize,
                PeriodicTaskIntervalSeconds = _config.UniqueKeysRefreshRate
            };

            _uniqueKeysTracker = new UniqueKeysTracker(config, adapter, trackerCache, senderAdapter, _tasksManager);
            
        }

        private void BuildImpressionManager()
        {
            var impressionsCounter = new ImpressionsCounter();
            var senderAdapter = new RedisSenderAdapter(_impressionsCache);

            _impressionCounterSender = new ImpressionsCountSender(senderAdapter, impressionsCounter, _tasksManager, interval: 300);
            _impressionsManager = new ImpressionsManager(_impressionsLog, _customerImpressionListener, impressionsCounter, false, _config.ImpressionsMode, telemetryRuntimeProducer: null, taskManager: _tasksManager, uniqueKeysTracker: _uniqueKeysTracker);
        }

        private void BuildEventLog()
        {
            var eventsCache = new RedisEventsCache(_redisAdapter, _config.SdkMachineName, _config.SdkMachineIP, _config.SdkVersion, _config.RedisUserPrefix);
            _eventsLog = new RedisEvenstLog(eventsCache);
        }
        
        private void BuildManager()
        {
            _manager = new SplitManager(_splitCache, _blockUntilReadyService);
        }

        private void BuildBlockUntilReadyService()
        {
            _blockUntilReadyService = new RedisBlockUntilReadyService(_redisAdapter);
        }

        private void BuildTelemetryStorage()
        {
            var redisTelemetryStorage = new RedisTelemetryStorage(_redisAdapter, _config.RedisUserPrefix, _config.SdkVersion, _config.SdkMachineIP, _config.SdkMachineName);

            _telemetryInitProducer = redisTelemetryStorage;
            _telemetryEvaluationProducer = redisTelemetryStorage;
        }

        private void RecordConfigInit()
        {
            var config = new Config
            {
                OperationMode = (int)_config.Mode,
                Storage = Constants.StorageType.Redis,
                ActiveFactories = _factoryInstantiationsService.GetActiveFactories(),
                RedundantActiveFactories = _factoryInstantiationsService.GetRedundantActiveFactories()
            };

            _telemetryInitProducer.RecordConfigInit(config);
        }

        private void Start()
        {
            _uniqueKeysTracker.Start();
            _impressionCounterSender.Start();
        }

        private static ISplitLogger GetLogger(ISplitLogger splitLogger = null)
        {
            return splitLogger ?? WrapperAdapter.GetLogger(typeof(RedisClient));
        }
        #endregion
    }
}
