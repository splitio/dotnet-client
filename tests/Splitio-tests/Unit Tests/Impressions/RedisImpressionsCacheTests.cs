﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Telemetry.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class RedisImpressionsCacheTests
    {
        private Mock<IRedisAdapterProducer> _redisAdapter;
        
        private IImpressionsCache _cache;

        [TestInitialize]
        public void Initialization()
        {
            _redisAdapter = new Mock<IRedisAdapterProducer>();
            var config = new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                RedisUserPrefix = "test-pre:",
                PoolSize = 1,
                SdkMachineIP = "ip",
                SdkVersion = "version",
                SdkMachineName = "mm"
            };

            _cache = new RedisImpressionsCache(_redisAdapter.Object, config, false);
        }

        [TestMethod]
        public async Task RecordUniqueKeysAndExpire()
        {
            var key = "test-pre:.SPLITIO.uniquekeys";
            var expected1 = "{\"f\":\"Feature1\",\"ks\":[\"key-1\",\"key-2\"]}";
            var expected2 = "{\"f\":\"Feature2\",\"ks\":[\"key-1\",\"key-2\"]}";
            
            _redisAdapter
                .Setup(mock => mock.ListRightPushAsync(key, expected1))
                .ReturnsAsync(1);

            _redisAdapter
                .Setup(mock => mock.ListRightPushAsync(key, expected2))
                .ReturnsAsync(2);

            await _cache.RecordUniqueKeysAsync(new List<Mtks>
            {
                new Mtks("Feature1", new HashSet<string>{ "key-1", "key-2" }),
                new Mtks("Feature2", new HashSet<string>{ "key-1", "key-2" })
            });
            
            _redisAdapter.Verify(mock => mock.ListRightPushAsync(key, expected1), Times.Once);
            _redisAdapter.Verify(mock => mock.ListRightPushAsync(key, expected2), Times.Once);
            _redisAdapter.Verify(mock => mock.KeyExpireAsync(key, new TimeSpan(0, 0, 3600)), Times.Once);
        }

        [TestMethod]
        public async Task RecordUniqueKeysWithoutExpire()
        {
            var key = "test-pre:.SPLITIO.uniquekeys";
            var expected1 = "{\"f\":\"Feature1\",\"ks\":[\"key-1\",\"key-2\"]}";
            var expected2 = "{\"f\":\"Feature2\",\"ks\":[\"key-1\",\"key-2\"]}";

            _redisAdapter
                .Setup(mock => mock.ListRightPushAsync(key, expected1))
                .ReturnsAsync(2);

            _redisAdapter
                .Setup(mock => mock.ListRightPushAsync(key, expected2))
                .ReturnsAsync(3);

            await _cache.RecordUniqueKeysAsync(new List<Mtks>
            {
                new Mtks("Feature1", new HashSet<string>{ "key-1", "key-2" }),
                new Mtks("Feature2", new HashSet<string>{ "key-1", "key-2" })
            });

            _redisAdapter.Verify(mock => mock.ListRightPushAsync(key, expected1), Times.Once);
            _redisAdapter.Verify(mock => mock.ListRightPushAsync(key, expected2), Times.Once);
            _redisAdapter.Verify(mock => mock.KeyExpireAsync(key, new TimeSpan(0, 0, 3600)), Times.Never);
        }
    }
}
