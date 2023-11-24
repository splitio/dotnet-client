﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Telemetry.Domain;
using Splitio_Tests.Resources;
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
            _impressionsCache = new RedisImpressionsCache(_redisAdapter, "ip", "version", "mm", RedisPrefix);
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

        private void Clean()
        { 
            var keys = _redisAdapter.Keys(RedisPrefix+"*");
            _redisAdapter.Del(keys);
        }
    }
}