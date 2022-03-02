using Splitio.Domain;
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
            BuildImpressionManager();
            BuildEventLog();
            BuildBlockUntilReadyService();
            BuildManager();
            BuildEvaluator();
        }

        #region Private Methods
        private void ReadConfig(ConfigurationOptions config)
        {            
            var baseConfig = _configService.ReadConfig(config, ConfingTypes.Redis);
            _config.SdkVersion = baseConfig.SdkVersion;
            _config.SdkMachineName = baseConfig.SdkMachineName;
            _config.SdkMachineIP = baseConfig.SdkMachineIP;
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
            var impressionsCache = new RedisImpressionsCache(_redisAdapter, _config.SdkMachineIP, _config.SdkVersion, _config.SdkMachineName, _config.RedisUserPrefix);
            _impressionsLog = new RedisImpressionLog(impressionsCache);

            _customerImpressionListener = config.ImpressionListener;
        }

        private void BuildUniqueKeysTracker()
        {
            var bloomFilter = new BloomFilter(expectedElements: 10000000, errorRate: 0.01);
            var adapter = new FilterAdapter(bloomFilter);
            var trackerCache = new ConcurrentDictionary<string, HashSet<string>>();
            _uniqueKeysTracker = new UniqueKeysTracker(adapter, trackerCache, 50000, null /*TODO: implement adapter for API*/, _tasksManager, periodicTaskIntervalSeconds: 3600);
        }

        private void BuildImpressionManager()
        {
            var impressionsCounter = new ImpressionsCounter();
            _impressionsManager = new ImpressionsManager(_impressionsLog, _customerImpressionListener, impressionsCounter, false, ImpressionsMode.Debug, telemetryRuntimeProducer: null, taskManager: _tasksManager, uniqueKeysTracker: _uniqueKeysTracker);
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

        private static ISplitLogger GetLogger(ISplitLogger splitLogger = null)
        {
            return splitLogger ?? WrapperAdapter.GetLogger(typeof(RedisClient));
        }
        #endregion
    }
}
