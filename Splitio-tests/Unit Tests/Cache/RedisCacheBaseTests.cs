﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisCacheBaseTests
    {
        [TestMethod]
        public async Task UseSplitioPrefix()
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

            var splitParser = new Mock<ISplitParser>();
            var redisAdapterMock = new Mock<IRedisAdapter>();
            var splitCache = new RedisSplitCache(redisAdapterMock.Object, splitParser.Object);

            redisAdapterMock
                .Setup(x => x.GetAsync("SPLITIO.split.test_split"))
                .ReturnsAsync(JsonConvert.SerializeObject(split));

            splitParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
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
            var result = await splitCache.GetSplitAsync(splitName);

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
        public async Task UseSplitioAndUserPrefix()
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

            var splitParser = new Mock<ISplitParser>();
            var redisAdapterMock = new Mock<IRedisAdapter>();            
            var splitCache = new RedisSplitCache(redisAdapterMock.Object, splitParser.Object, "mycompany");

            redisAdapterMock
                .Setup(x => x.GetAsync("mycompany.SPLITIO.split.test_split"))
                .ReturnsAsync(JsonConvert.SerializeObject(split));

            splitParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
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
            var result = await splitCache.GetSplitAsync(splitName);

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
