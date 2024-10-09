using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using StackExchange.Redis;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisImpressionCacheTests
    {
        [TestMethod]
        public void AddImpressionSuccessfully()
        {
            //Arrange
            var key = "SPLITIO.impressions";
            var redisAdapterMock = new Mock<IRedisAdapterProducer>();
            var config = new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 1,
                SdkMachineIP = "10.0.0.1",
                SdkVersion = "net-1.0.2",
                SdkMachineName = "machine_name_test"
            };
            var cache = new RedisImpressionsCache(redisAdapterMock.Object, config, false);
            var impressions = new List<KeyImpression>
            {
                new KeyImpression { feature = "test", changeNumber = 100, keyName = "date", label = "testdate", time = 10000000 }
            };

            //Act
            cache.Add(impressions);

            //Assert
            redisAdapterMock.Verify(mock => mock.ListRightPush(key, It.IsAny<RedisValue[]>()));
        }
    }
}
