using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisSplitCacheAsyncTests
    {
        private const string SplitKeyPrefix = "SPLITIO.split.";
        private const string SplitsKeyPrefix = "SPLITIO.splits.";
        private const string TrafficTypeKeyPrefix = "SPLITIO.trafficType.";

        private readonly Mock<IRedisAdapter> _redisAdapterMock;
        private readonly Mock<ISplitParser> _splitParserMock;
        private readonly ISplitCache _redisSplitCache;

        public RedisSplitCacheAsyncTests()
        {
            _redisAdapterMock = new Mock<IRedisAdapter>();
            _splitParserMock = new Mock<ISplitParser>();

            _redisSplitCache = new RedisSplitCache(_redisAdapterMock.Object, _splitParserMock.Object);
        }

        [TestMethod]
        public async Task GetInexistentSplitOrRedisExceptionShouldReturnNull()
        {
            //Arrange
            var splitName = "test_split";
            string value = null;

            _redisAdapterMock
                .Setup(x => x.GetAsync(SplitKeyPrefix + splitName))
                .ReturnsAsync(value);

            //Act
            var result = await _redisSplitCache.GetSplitAsync(splitName);

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task RemoveSplit_ReturnsFalse()
        {
            //Act
            var result = await _redisSplitCache.RemoveSplitAsync("splitName");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task GetChangeNumberWhenNotSetOrRedisThrowsException()
        {
            //Arrange
            var changeNumber = -1;

            _redisAdapterMock
                .Setup(x => x.GetAsync(SplitsKeyPrefix + "till"))
                .ReturnsAsync(string.Empty);

            //Act
            var result = await _redisSplitCache.GetChangeNumberAsync();

            //Assert
            Assert.AreEqual(changeNumber, result);
        }

        [TestMethod]
        public async Task GetAllSplitsSuccessfully()
        {
            //Arrange
            var splitName = "test_split";
            var splitName2 = "test_split2";
            var splitJson = JsonConvert.SerializeObject(BuildSplit(splitName));
            var splitJson2 = JsonConvert.SerializeObject(BuildSplit(splitName2));

            _redisAdapterMock
                .Setup(x => x.Keys(SplitKeyPrefix + "*"))
                .Returns(new RedisKey[] { splitName, splitName2 });

            _redisAdapterMock
                .Setup(x => x.MGetAsync(It.IsAny<RedisKey[]>()))
                .ReturnsAsync(new RedisValue[] { splitJson, splitJson2 });

            _splitParserMock
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit());

            //Act
            var result = await _redisSplitCache.GetAllSplitsAsync();

            //Assert
            Assert.AreEqual(2, result.Count);
        }

        //[TestMethod]
        //public async Task GetAllSplitsShouldReturnEmptyListIfGetReturnsEmpty()
        //{
        //    //Arrange
        //    _redisAdapterMock
        //        .Setup(x => x.Keys(splitKeyPrefix + "*"))
        //        .Returns(Array.Empty<RedisKey>());

        //    _redisAdapterMock
        //        .Setup(x => x.MGet(It.IsAny<RedisKey[]>()))
        //        .Returns(Array.Empty<RedisValue>());

        //    //Act
        //    var result = _redisSplitCache.GetAllSplits();

        //    //Assert
        //    Assert.IsNotNull(result);
        //    Assert.AreEqual(0, result.Count);
        //}

        //[TestMethod]
        //public async Task GetAllSplitsShouldReturnEmptyListIfGetReturnsNull()
        //{
        //    //Arrange
        //    RedisValue[] expectedResult = null;

        //    _redisAdapterMock
        //        .Setup(x => x.Keys(splitKeyPrefix + "*"))
        //        .Returns(Array.Empty<RedisKey>());

        //    _redisAdapterMock
        //        .Setup(x => x.MGet(It.IsAny<RedisKey[]>()))
        //        .Returns(expectedResult);

        //    //Act
        //    var result = _redisSplitCache.GetAllSplits();

        //    //Assert
        //    Assert.IsNotNull(result);
        //    Assert.AreEqual(0, result.Count);
        //}

        //[TestMethod]
        //public async Task GetKeysTestSuccessfully()
        //{
        //    //Arrange
        //    _redisAdapterMock
        //        .Setup(x => x.Keys(splitKeyPrefix + "*"))
        //        .Returns(new RedisKey[] { "test_split", "test_split2" });

        //    //Act
        //    var result = _redisSplitCache.GetKeys();

        //    //Assert
        //    Assert.IsNotNull(result);
        //    Assert.AreEqual(2, result.Count);
        //}

        //[TestMethod]
        //public async Task GetKeysShouldReturnEmptyResultIfNoKeysOrRedisException()
        //{
        //    //Arrange
        //    _redisAdapterMock
        //        .Setup(x => x.Keys(splitKeyPrefix + "*"))
        //        .Returns(Array.Empty<RedisKey>());

        //    //Act
        //    var result = _redisSplitCache.GetKeys();

        //    //Assert
        //    Assert.IsNotNull(result);
        //    Assert.AreEqual(0, result.Count);
        //}

        //#region TrafficTypeExists
        //[TestMethod]
        //public async Task TrafficTypeExists_WhenHasQuantity_ReturnsTrue()
        //{
        //    //Arrange
        //    var trafficType = "test";

        //    var ttKey = $"{trafficTypeKeyPrefix}{trafficType}";

        //    _redisAdapterMock
        //        .Setup(mock => mock.Get(ttKey))
        //        .Returns("1");

        //    //Act
        //    var result = _redisSplitCache.TrafficTypeExists(trafficType);

        //    //Assert
        //    Assert.IsTrue(result);
        //}

        //[TestMethod]
        //public async Task TrafficTypeExists_WhenQuantityIs0_ReturnsFalse()
        //{
        //    //Arrange
        //    var trafficType = "test";

        //    var ttKey = $"{trafficTypeKeyPrefix}{trafficType}";

        //    _redisAdapterMock
        //        .Setup(mock => mock.Get(ttKey))
        //        .Returns("0");

        //    //Act
        //    var result = _redisSplitCache.TrafficTypeExists(trafficType);

        //    //Assert
        //    Assert.IsFalse(result);
        //}

        //[TestMethod]
        //public async Task TrafficTypeExists_WhenKeyDoesNotExist_ReturnsFalse()
        //{
        //    //Arrange
        //    var trafficType = "test";

        //    var ttKey = $"{trafficTypeKeyPrefix}{trafficType}";

        //    _redisAdapterMock
        //        .Setup(mock => mock.Get(ttKey))
        //        .Returns((string)null);

        //    //Act
        //    var result = _redisSplitCache.TrafficTypeExists(trafficType);

        //    //Assert
        //    Assert.IsFalse(result);
        //}

        //[TestMethod]
        //public async Task TrafficTypeExists_WhenValueIsEmpty_ReturnsFalse()
        //{
        //    //Arrange
        //    var trafficType = "test";

        //    var ttKey = $"{trafficTypeKeyPrefix}{trafficType}";

        //    _redisAdapterMock
        //        .Setup(mock => mock.Get(ttKey))
        //        .Returns(string.Empty);

        //    //Act
        //    var result = _redisSplitCache.TrafficTypeExists(trafficType);

        //    //Assert
        //    Assert.IsFalse(result);
        //}

        //[TestMethod]
        //public async Task TrafficTypeExists_WhenKeyIsNull_ReturnsFalse()
        //{
        //    //Arrange
        //    var trafficType = "test";

        //    var ttKey = $"{trafficTypeKeyPrefix}{trafficType}";

        //    _redisAdapterMock
        //        .Setup(mock => mock.Get(ttKey))
        //        .Returns(string.Empty);

        //    //Act
        //    var result = _redisSplitCache.TrafficTypeExists(null);

        //    //Assert
        //    Assert.IsFalse(result);
        //}
        //#endregion

        //[TestMethod]
        //public async Task FetchMany_VerifyMGetCall_Once()
        //{
        //    // Arrange.
        //    var splitNames = new List<string> { "Split_1", "Split_2", "Split_3" };

        //    _redisAdapterMock
        //        .Setup(mock => mock.MGet(It.IsAny<RedisKey[]>()))
        //        .Returns(new RedisValue[3]);

        //    // Act.
        //    var result = _redisSplitCache.FetchMany(splitNames);

        //    // Assert.
        //    _redisAdapterMock.Verify(mock => mock.MGet(It.IsAny<RedisKey[]>()), Times.Once);
        //    _redisAdapterMock.Verify(mock => mock.Get(It.IsAny<string>()), Times.Never);
        //}

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
