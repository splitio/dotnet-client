using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Interfaces;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisCacheBaseTests
    {
        [TestMethod]
        public void UseSplitioPrefix()
        {
            //Arrange
            var splitName = "test_split";
            var split = new Split
            {
                name = splitName,
                changeNumber = 121291,
                killed = false,
                seed = 4324324,
                defaultTreatment = "on",
                conditions = new List<ConditionDefinition>(),
                status = "ACTIVE",
                trafficTypeName = "test"
            };
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
            };
            var splitParser = new Mock<IParser<Split, ParsedSplit>>();
            var redisAdapterMock = new Mock<IRedisAdapterConsumer>();
            var rbsParser = new Mock<IRuleBasedSegmentCacheConsumer>();
            var splitCache = new RedisSplitCache(redisAdapterMock.Object, splitParser.Object, config, false, rbsParser.Object);

            redisAdapterMock
                .Setup(x => x.Get("SPLITIO.split.test_split"))
                .Returns(JsonConvert.SerializeObject(split));

            splitParser
                .Setup(mock => mock.Parse(It.IsAny<Split>(), rbsParser.Object))
                .Returns(new ParsedSplit
                {
                    name = split.name,
                    changeNumber = split.changeNumber,
                    killed = split.killed,
                    seed = split.seed,
                    defaultTreatment = split.defaultTreatment,
                    trafficTypeName = split.trafficTypeName
                });

            //Act
            var result = splitCache.GetSplit(splitName);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(split.name, result.name);
            Assert.AreEqual(split.changeNumber, result.changeNumber);
            Assert.AreEqual(split.killed, result.killed);
            Assert.AreEqual(split.seed, result.seed);
            Assert.AreEqual(split.defaultTreatment, result.defaultTreatment);
            Assert.AreEqual(split.trafficTypeName, result.trafficTypeName);
        }

        [TestMethod]
        public void UseSplitioAndUserPrefix()
        {
            //Arrange
            var splitName = "test_split";
            var split = new Split
            {
                name = splitName,
                changeNumber = 121291,
                killed = false,
                seed = 4324324,
                defaultTreatment = "on",
                conditions = new List<ConditionDefinition>(),
                status = "ACTIVE",
                trafficTypeName = "test"
            };
            var config = new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                RedisUserPrefix = "mycompany",
                PoolSize = 1,
            };
            var splitParser = new Mock<IParser<Split, ParsedSplit>>();
            var redisAdapterMock = new Mock<IRedisAdapterConsumer>();
            var rbsParser = new Mock<IRuleBasedSegmentCacheConsumer>();
            var splitCache = new RedisSplitCache(redisAdapterMock.Object, splitParser.Object, config, false, rbsParser.Object);

            redisAdapterMock
                .Setup(x => x.Get("mycompany.SPLITIO.split.test_split"))
                .Returns(JsonConvert.SerializeObject(split));

            splitParser
                .Setup(mock => mock.Parse(It.IsAny<Split>(), rbsParser.Object))
                .Returns(new ParsedSplit
                {
                    name = split.name,
                    changeNumber = split.changeNumber,
                    killed = split.killed,
                    seed = split.seed,
                    defaultTreatment = split.defaultTreatment,
                    trafficTypeName = split.trafficTypeName
                });

            //Act
            var result = splitCache.GetSplit(splitName);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(split.name, result.name);
            Assert.AreEqual(split.changeNumber, result.changeNumber);
            Assert.AreEqual(split.killed, result.killed);
            Assert.AreEqual(split.seed, result.seed);
            Assert.AreEqual(split.defaultTreatment, result.defaultTreatment);
            Assert.AreEqual(split.trafficTypeName, result.trafficTypeName);
        }
    }
}
