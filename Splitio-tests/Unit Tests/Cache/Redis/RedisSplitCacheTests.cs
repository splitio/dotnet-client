using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisSplitCacheTests
    {
        private const string splitKeyPrefix = "SPLITIO.split.";
        private const string splitsKeyPrefix = "SPLITIO.splits.";
        private const string trafficTypeKeyPrefix = "SPLITIO.trafficType.";

        private readonly Mock<IRedisAdapter> _redisAdapterMock;
        private readonly Mock<ISplitParser> _splitParserMock;
        private readonly RedisSplitCache _redisSplitCache;

        public RedisSplitCacheTests()
        {
            _redisAdapterMock = new Mock<IRedisAdapter>();
            _splitParserMock = new Mock<ISplitParser>();

            _redisSplitCache = new RedisSplitCache(_redisAdapterMock.Object, _splitParserMock.Object);
        }

        [TestMethod]
        public void GetInexistentSplitOrRedisExceptionShouldReturnNull()
        {
            //Arrange
            var splitName = "test_split";
            string value = null;

            _redisAdapterMock
                .Setup(x => x.Get(splitKeyPrefix + "test_split"))
                .Returns(value);

            //Act
            var result = _redisSplitCache.GetSplit(splitName);

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetChangeNumberWhenNotSetOrRedisThrowsException()
        {
            //Arrange
            var changeNumber = -1;

            _redisAdapterMock
                .Setup(x => x.Get(splitsKeyPrefix + "till"))
                .Returns(string.Empty);

            //Act
            var result = _redisSplitCache.GetChangeNumber();

            //Assert
            Assert.AreEqual(changeNumber, result);
        }

        [TestMethod]
        public void GetAllSplitsSuccessfully()
        {
            //Arrange
            var splitName = "test_split";
            var splitName2 = "test_split2";
            var split = BuildSplit(splitName);
            var split2 = BuildSplit(splitName2);
            var splitJson = JsonConvert.SerializeObject(split);
            var splitJson2 = JsonConvert.SerializeObject(split2);

            _redisAdapterMock
                .Setup(x => x.Keys(splitKeyPrefix + "*"))
                .Returns(new RedisKey[] { "test_split", "test_split2" });

            _redisAdapterMock
                .Setup(x => x.MGet(It.IsAny<RedisKey[]>()))
                .Returns(new RedisValue[] { splitJson, splitJson2 });

            _splitParserMock
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit());                

            //Act
            var result = _redisSplitCache.GetAllSplits();

            //Assert
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void GetAllSplitsShouldReturnEmptyListIfGetReturnsEmpty()
        {
            //Arrange
            _redisAdapterMock
                .Setup(x => x.Keys(splitKeyPrefix + "*"))
                .Returns(Array.Empty<RedisKey>());

            _redisAdapterMock
                .Setup(x => x.MGet(It.IsAny<RedisKey[]>()))
                .Returns(Array.Empty<RedisValue>());

            //Act
            var result = _redisSplitCache.GetAllSplits();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetAllSplitsShouldReturnEmptyListIfGetReturnsNull()
        {
            //Arrange
            RedisValue[] expectedResult = null;

            _redisAdapterMock
                .Setup(x => x.Keys(splitKeyPrefix + "*"))
                .Returns(Array.Empty<RedisKey>());

            _redisAdapterMock
                .Setup(x => x.MGet(It.IsAny<RedisKey[]>()))
                .Returns(expectedResult);

            //Act
            var result = _redisSplitCache.GetAllSplits();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        #region TrafficTypeExists
        [TestMethod]
        public void TrafficTypeExists_WhenHasQuantity_ReturnsTrue()
        {
            //Arrange
            var trafficType = "test";

            var ttKey = $"{trafficTypeKeyPrefix}{trafficType}";

            _redisAdapterMock
                .Setup(mock => mock.Get(ttKey))
                .Returns("1");

            //Act
            var result = _redisSplitCache.TrafficTypeExists(trafficType);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TrafficTypeExists_WhenQuantityIs0_ReturnsFalse()
        {
            //Arrange
            var trafficType = "test";

            var ttKey = $"{trafficTypeKeyPrefix}{trafficType}";

            _redisAdapterMock
                .Setup(mock => mock.Get(ttKey))
                .Returns("0");

            //Act
            var result = _redisSplitCache.TrafficTypeExists(trafficType);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TrafficTypeExists_WhenKeyDoesNotExist_ReturnsFalse()
        {
            //Arrange
            var trafficType = "test";

            var ttKey = $"{trafficTypeKeyPrefix}{trafficType}";

            _redisAdapterMock
                .Setup(mock => mock.Get(ttKey))
                .Returns((string)null);

            //Act
            var result = _redisSplitCache.TrafficTypeExists(trafficType);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TrafficTypeExists_WhenValueIsEmpty_ReturnsFalse()
        {
            //Arrange
            var trafficType = "test";

            var ttKey = $"{trafficTypeKeyPrefix}{trafficType}";

            _redisAdapterMock
                .Setup(mock => mock.Get(ttKey))
                .Returns(string.Empty);

            //Act
            var result = _redisSplitCache.TrafficTypeExists(trafficType);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TrafficTypeExists_WhenKeyIsNull_ReturnsFalse()
        {
            //Arrange
            var trafficType = "test";

            var ttKey = $"{trafficTypeKeyPrefix}{trafficType}";

            _redisAdapterMock
                .Setup(mock => mock.Get(ttKey))
                .Returns(string.Empty);

            //Act
            var result = _redisSplitCache.TrafficTypeExists(null);

            //Assert
            Assert.IsFalse(result);
        }
        #endregion

        [TestMethod]
        public void FetchMany_VerifyMGetCall_Once()
        {
            // Arrange.
            var splitNames = new List<string> { "Split_1", "Split_2", "Split_3" };

            _redisAdapterMock
                .Setup(mock => mock.MGet(It.IsAny<RedisKey[]>()))
                .Returns(new RedisValue[3]);

            // Act.
            var result = _redisSplitCache.FetchMany(splitNames);

            // Assert.
            _redisAdapterMock.Verify(mock => mock.MGet(It.IsAny<RedisKey[]>()), Times.Once);
            _redisAdapterMock.Verify(mock => mock.Get(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void GetNamesByFlagSetsReturnsValues()
        {
            // Arrange.
            var flagSetNames = new List<string> { "set1", "set2", "set3", "set4" };

            _redisAdapterMock
                .Setup(mock => mock.SMembers("SPLITIO.flagSet.set1"))
                .Returns(new RedisValue[3] { "flag-name1" ,"flag-name2", "flag-name3" });

            _redisAdapterMock
                .Setup(mock => mock.SMembers("SPLITIO.flagSet.set2"))
                .Returns(new RedisValue[3] { "flag-name1", "flag-name2", "flag-name3" });

            _redisAdapterMock
                .Setup(mock => mock.SMembers("SPLITIO.flagSet.set3"))
                .Returns(new RedisValue[3] { "flag-name1", "flag-name2", "flag-name3" });

            _redisAdapterMock
                .Setup(mock => mock.SMembers("SPLITIO.flagSet.set4"))
                .Returns(Array.Empty<RedisValue>());

            // Act.
            var result = _redisSplitCache.GetNamesByFlagSets(flagSetNames);

            // Assert.
            Assert.AreEqual(4, result.Count);
            var set1 = result["set1"];
            Assert.AreEqual(3, set1.Count);
            var set2 = result["set2"];
            Assert.AreEqual(3, set2.Count);
            var set3 = result["set3"];
            Assert.AreEqual(3, set3.Count);
            var set4 = result["set4"];
            Assert.IsFalse(set4.Any());
        }

        private static Split BuildSplit(string splitName)
        {
            return new Split
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
        }
    }
}
