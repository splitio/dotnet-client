using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Integration_tests.Resources;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Integration_tests.Async
{
    [TestClass]
    public class RedisClientAsyncTests : BaseAsyncClientTests
    {
        private const string Host = "localhost";
        private const string Port = "6379";
        private const string Password = "";
        private const int Database = 0;
        private const string UserPrefix = "prefix-test-async";

        private RedisAdapterForTests _redisAdapter;
        private string rootFilePath;

        public RedisClientAsyncTests() : base("Redis")
        {
        }

        [TestInitialize]
        public async Task Init()
        {
            var config = new RedisConfig
            {
                RedisHost = Host,
                RedisPort = Port,
                RedisPassword = Password,
                RedisDatabase = Database,
                PoolSize = 1,
                RedisUserPrefix = UserPrefix,
            };
            var pool = new ConnectionPoolManager(config);
            _redisAdapter = new RedisAdapterForTests(config, pool);

            rootFilePath = string.Empty;

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif

            await RedisHelper.LoadSplitsAsync(rootFilePath, UserPrefix, _redisAdapter);
        }

        protected override async Task AssertSentEventsAsync(List<EventBackend> eventsExcpected, int sleepTime = 15000, int? eventsCount = null, bool validateEvents = true)
        {
            await RedisHelper.AssertSentEventsAsync(_redisAdapter, UserPrefix, eventsExcpected, sleepTime, eventsCount, validateEvents);
        }

        protected override async Task AssertSentImpressionsAsync(int sentImpressionsCount, params KeyImpression[] expectedImpressions)
        {
            await RedisHelper.AssertSentImpressionsAsync(_redisAdapter, UserPrefix, sentImpressionsCount, expectedImpressions);
        }

        protected override async Task CleanupAsync()
        {
            await RedisHelper.CleanupAsync(UserPrefix, _redisAdapter);
        }

        protected override async Task DelayAsync()
        {
            await Task.Delay(1000);
        }

        protected override ConfigurationOptions GetConfigurationOptions(int? eventsPushRate = null, int? eventsQueueSize = null, int? featuresRefreshRate = null, bool? ipAddressesEnabled = null, IImpressionListener impressionListener = null)
        {
            var cacheConfig = new CacheAdapterConfigurationOptions
            {
                Host = Host,
                Port = Port,
                Password = Password,
                Database = Database,
                UserPrefix = UserPrefix
            };

            return new ConfigurationOptions
            {
                ImpressionListener = impressionListener,
                FeaturesRefreshRate = featuresRefreshRate ?? 1,
                SegmentsRefreshRate = 1,
                ImpressionsRefreshRate = 1,
                EventsPushRate = eventsPushRate ?? 1,
                IPAddressesEnabled = ipAddressesEnabled,
                CacheAdapterConfig = cacheConfig,
                Mode = Mode.Consumer
            };
        }
    }
}
