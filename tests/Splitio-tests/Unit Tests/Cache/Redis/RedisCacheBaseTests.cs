using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
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
                Name = splitName,
                ChangeNumber = 121291,
                Killed = false,
                Seed = 4324324,
                DefaultTreatment = "on",
                Conditions = new List<Condition>(),
                Status = "ACTIVE",
                TrafficTypeName = "test"
            };

            var splitParser = new Mock<ISplitParser>();
            var redisAdapterMock = new Mock<IRedisAdapter>();
            var splitCache = new RedisSplitCache(redisAdapterMock.Object, splitParser.Object);

            redisAdapterMock
                .Setup(x => x.Get("SPLITIO.split.test_split"))
                .Returns(JsonConvert.SerializeObject(split));

            splitParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit
                {
                    Name = split.Name,
                    ChangeNumber = split.ChangeNumber,
                    Killed = split.Killed,
                    Seed = split.Seed,
                    DefaultTreatment = split.DefaultTreatment,
                    TrafficTypeName = split.TrafficTypeName
                });

            //Act
            var result = splitCache.GetSplit(splitName);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(split.Name, result.Name);
            Assert.AreEqual(split.ChangeNumber, result.ChangeNumber);
            Assert.AreEqual(split.Killed, result.Killed);
            Assert.AreEqual(split.Seed, result.Seed);
            Assert.AreEqual(split.DefaultTreatment, result.DefaultTreatment);
            Assert.AreEqual(split.TrafficTypeName, result.TrafficTypeName);
        }

        [TestMethod]
        public void UseSplitioAndUserPrefix()
        {
            //Arrange
            var splitName = "test_split";
            var split = new Split
            {
                Name = splitName,
                ChangeNumber = 121291,
                Killed = false,
                Seed = 4324324,
                DefaultTreatment = "on",
                Conditions = new List<Condition>(),
                Status = "ACTIVE",
                TrafficTypeName = "test"
            };

            var splitParser = new Mock<ISplitParser>();
            var redisAdapterMock = new Mock<IRedisAdapter>();            
            var splitCache = new RedisSplitCache(redisAdapterMock.Object, splitParser.Object, "mycompany");

            redisAdapterMock
                .Setup(x => x.Get("mycompany.SPLITIO.split.test_split"))
                .Returns(JsonConvert.SerializeObject(split));

            splitParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit
                {
                    Name = split.Name,
                    ChangeNumber = split.ChangeNumber,
                    Killed = split.Killed,
                    Seed = split.Seed,
                    DefaultTreatment = split.DefaultTreatment,
                    TrafficTypeName = split.TrafficTypeName
                });

            //Act
            var result = splitCache.GetSplit(splitName);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(split.Name, result.Name);
            Assert.AreEqual(split.ChangeNumber, result.ChangeNumber);
            Assert.AreEqual(split.Killed, result.Killed);
            Assert.AreEqual(split.Seed, result.Seed);
            Assert.AreEqual(split.DefaultTreatment, result.DefaultTreatment);
            Assert.AreEqual(split.TrafficTypeName, result.TrafficTypeName);
        }
    }
}
