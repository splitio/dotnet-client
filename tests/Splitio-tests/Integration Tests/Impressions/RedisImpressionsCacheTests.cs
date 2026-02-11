using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Telemetry.Domain;
using Splitio.Tests.Common.Resources;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_Tests.Integration_Tests.Impressions
{
    [TestClass]
    public class RedisImpressionsCacheTests
    {
        private const string RedisPrefix = "test-mtks:";

        private readonly RedisAdapterForTests _redisAdapter;
        private readonly IImpressionsCache _impressionsCache;

        public RedisImpressionsCacheTests()
        {
            var config = new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                RedisUserPrefix = RedisPrefix,
                PoolSize = 1,
                SdkMachineIP = "ip",
                SdkVersion = "version",
                SdkMachineName = "mm"
            };
            var connectionPoolManager = new ConnectionPoolManager(config);

            _redisAdapter = new RedisAdapterForTests(config, connectionPoolManager);
            var redisProducer = new RedisAdapterProducer(config, connectionPoolManager);
            _impressionsCache = new RedisImpressionsCache(redisProducer, config, false);
        }

        [TestMethod]
        public async Task RecordUniqueKeysAndExpire()
        {
            await _impressionsCache.RecordUniqueKeysAsync(new List<Mtks>
            {
                new Mtks("Feature1", new HashSet<string>{ "key-1", "key-2" }),
                new Mtks("Feature2", new HashSet<string>{ "key-1", "key-2" })
            });

            await _impressionsCache.RecordUniqueKeysAsync(new List<Mtks>
            {
                new Mtks("Feature1", new HashSet<string>{ "key-1", "key-2" }),
                new Mtks("Feature2", new HashSet<string>{ "key-1", "key-2" })
            });

            var key = $"{RedisPrefix}.SPLITIO.uniquekeys";
            var keys = _redisAdapter.ListRange(key);
            var keyTimeToLive = _redisAdapter.KeyTimeToLive(key);
            
            Assert.AreEqual(4, keys.Length);
            Assert.IsNotNull(keyTimeToLive);

            Clean();
        }

        [TestMethod]
        public async Task RecordUniqueKeysAndExpireRedisCluster()
        {
            var redisAdapter = GetRedisClusterAdapter();
            var impressionsCache = GetRedisClusterImpressionsCache();
            Clean();
            await impressionsCache.RecordUniqueKeysAsync(new List<Mtks>
            {
                new Mtks("Feature1", new HashSet<string>{ "key-1", "key-2" }),
                new Mtks("Feature2", new HashSet<string>{ "key-1", "key-2" })
            });

            await impressionsCache.RecordUniqueKeysAsync(new List<Mtks>
            {
                new Mtks("Feature1", new HashSet<string>{ "key-1", "key-2" }),
                new Mtks("Feature2", new HashSet<string>{ "key-1", "key-2" })
            });

            var key = $"{{SPLITIO}}{RedisPrefix}.SPLITIO.uniquekeys";
            var keys = redisAdapter.ListRange(key);
            var keyTimeToLive = redisAdapter.KeyTimeToLive(key);

            Assert.AreEqual(4, keys.Length);
            Assert.IsNotNull(keyTimeToLive);

            Clean();
        }

        [TestMethod]
        public async Task RecordImpressionWithProperties()
        {
            // Arrange.
            var cache = GetRedisClusterImpressionsCache("Prop-Test:");
            var impressions = new List<KeyImpression>
            {
                new KeyImpression("matching-key", "feature-1", "treatment", 34534546, 3333444, "label", "bucketing-key", false),
                new KeyImpression("matching-key", "feature-1", "treatment", 34534550, 3333444, "label", "bucketing-key", false, 34534546),
                new KeyImpression("matching-key", "feature-2", "treatment", 34534546, 3333444, "label", "bucketing-key", false, properties: "{\"prop\":\"val\"}"),
            };

            // Act.
            await _impressionsCache.AddAsync(impressions);
            var result = await _redisAdapter.ListRangeAsync("test-mtks:.SPLITIO.impressions");

            // Assert.
            Assert.AreEqual(3, result.Count());
            Assert.AreEqual("{\"m\":{\"s\":\"version\",\"i\":\"ip\",\"n\":\"mm\"},\"i\":{\"f\":\"feature-1\",\"k\":\"matching-key\",\"t\":\"treatment\",\"m\":34534546,\"c\":3333444,\"r\":\"label\",\"b\":\"bucketing-key\",\"pt\":null}}", result[0].ToString());
            Assert.AreEqual("{\"m\":{\"s\":\"version\",\"i\":\"ip\",\"n\":\"mm\"},\"i\":{\"f\":\"feature-1\",\"k\":\"matching-key\",\"t\":\"treatment\",\"m\":34534550,\"c\":3333444,\"r\":\"label\",\"b\":\"bucketing-key\",\"pt\":34534546}}", result[1].ToString());
            Assert.AreEqual("{\"m\":{\"s\":\"version\",\"i\":\"ip\",\"n\":\"mm\"},\"i\":{\"f\":\"feature-2\",\"k\":\"matching-key\",\"t\":\"treatment\",\"m\":34534546,\"c\":3333444,\"r\":\"label\",\"b\":\"bucketing-key\",\"pt\":null,\"properties\":\"{\\\"prop\\\":\\\"val\\\"}\"}}", result[2].ToString());

            Clean();
        }

        private static RedisAdapterForTests GetRedisClusterAdapter(string prefix = null)
        {
            var config = new RedisConfig
            {
                ClusterNodes = new ClusterNodes( new List<string>() { "localhost:6379" }, "{SPLITIO}"),
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 1,
                RedisUserPrefix = prefix ?? RedisPrefix
            };

            var pool = new ConnectionPoolManager(config);
            return new RedisAdapterForTests(config, pool);
        }

        private static RedisImpressionsCache GetRedisClusterImpressionsCache(string prefix = null)
        {
            var config = new RedisConfig
            {
                ClusterNodes = new ClusterNodes(new List<string>() { "localhost:6379" }, "{SPLITIO}"),
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 1,
                RedisUserPrefix = prefix ?? RedisPrefix,
                SdkMachineIP = "ip",
                SdkVersion = "version",
                SdkMachineName = "mm"
            };

            var pool = new ConnectionPoolManager(config);
            var redisProducer = new RedisAdapterProducer(config, pool);
            return new RedisImpressionsCache(redisProducer, config, true);
        }

        private void Clean()
        { 
            var keys = _redisAdapter.Keys(RedisPrefix+"*");
            _redisAdapter.Del(keys);

            keys = _redisAdapter.Keys("{SPLITIO}" + RedisPrefix + "*");
            _redisAdapter.Del(keys);
        }
    }
}
