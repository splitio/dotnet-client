using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Tests.Common;
using Splitio.Tests.Common.Resources;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Integration_redis_tests
{
    [TestClass, TestCategory("Integration")]
    public class RedisClusterClientAsyncTests : BaseAsyncClientTests
    {
        private const string Host = "localhost";
        private const string Port = "6379";
        private const string Password = "";
        private const int Database = 0;
        private const string UserPrefix = "{SPLITIO}prefix-test-async";

        private RedisAdapterForTests _redisAdapter;
        private string rootFilePath;

        public RedisClusterClientAsyncTests() : base("Redis")
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

        protected override void AssertSentEvents(List<EventBackend> eventsExcpected, int? eventsCount = null, bool validateEvents = true)
        {
            RedisHelper.AssertSentEvents(_redisAdapter, UserPrefix, eventsExcpected, eventsCount, validateEvents);
        }

        protected override void AssertSentImpressions(int sentImpressionsCount, params KeyImpression[] expectedImpressions)
        {
            RedisHelper.AssertSentImpressions(_redisAdapter, UserPrefix, sentImpressionsCount, expectedImpressions);
        }

        protected override async Task CleanupAsync()
        {
            await RedisHelper.CleanupAsync(UserPrefix, _redisAdapter);
        }

        protected override ConfigurationOptions GetConfigurationOptions(int? eventsPushRate = null, int? eventsQueueSize = null, int? featuresRefreshRate = null, bool? ipAddressesEnabled = null, IImpressionListener impressionListener = null)
        {
            var clusterNodes = new ClusterNodes(new List<string>() { Host + ":" + Port }, "{SPLITIO}");
            var cacheConfig = new CacheAdapterConfigurationOptions
            {
                RedisClusterNodes = clusterNodes,
                Password = Password,
                Database = Database,
                UserPrefix = "prefix-test-async"
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
