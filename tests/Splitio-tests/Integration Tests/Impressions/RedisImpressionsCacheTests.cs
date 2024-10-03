using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Telemetry.Domain;
using Splitio.Tests.Common.Resources;
using System.Collections.Generic;
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
            };
            var connectionPoolManager = new ConnectionPoolManager(config);

            _redisAdapter = new RedisAdapterForTests(config, connectionPoolManager);
            var redisProducer = new RedisAdapterProducer(config, connectionPoolManager);
            _impressionsCache = new RedisImpressionsCache(redisProducer, "ip", "version", "mm", RedisPrefix);
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

        private static RedisAdapterForTests GetRedisClusterAdapter()
        {
            var config = new RedisConfig
            {
                ClusterNodes = new Splitio.Domain.ClusterNodes( new List<string>() { "localhost:6379" }, "{SPLITIO}"),
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 1,
                RedisUserPrefix = RedisPrefix
            };

            var pool = new ConnectionPoolManager(config);
            return new RedisAdapterForTests(config, pool);
        }

        private static RedisImpressionsCache GetRedisClusterImpressionsCache()
        {
            var config = new RedisConfig
            {
                ClusterNodes = new Splitio.Domain.ClusterNodes(new List<string>() { "localhost:6379" }, "{SPLITIO}"),
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 1,
                RedisUserPrefix = RedisPrefix
            };

            var pool = new ConnectionPoolManager(config);
            var redisProducer = new RedisAdapterProducer(config, pool);
            return new RedisImpressionsCache(redisProducer, "ip", "version", "mm", "{SPLITIO}" + RedisPrefix);
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
