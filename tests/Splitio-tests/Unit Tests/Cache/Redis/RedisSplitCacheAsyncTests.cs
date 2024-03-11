using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisSplitCacheAsyncTests
    {
        private const string SplitKeyPrefix = "SPLITIO.split.";

        private readonly Mock<IRedisAdapter> _redisAdapterMock;
        private readonly Mock<ISplitParser> _splitParserMock;
        private readonly IFeatureFlagCacheConsumer _redisSplitCache;

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

        [TestMethod]
        public async Task GetAllSplitsShouldReturnEmptyListIfGetReturnsEmpty()
        {
            //Arrange
            _redisAdapterMock
                .Setup(x => x.Keys(SplitKeyPrefix + "*"))
                .Returns(Array.Empty<RedisKey>());

            _redisAdapterMock
                .Setup(x => x.MGetAsync(It.IsAny<RedisKey[]>()))
                .ReturnsAsync(Array.Empty<RedisValue>());

            //Act
            var result = await _redisSplitCache.GetAllSplitsAsync();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task GetAllSplitsShouldReturnEmptyListIfGetReturnsNull()
        {
            //Arrange
            RedisValue[] expectedResult = null;

            _redisAdapterMock
                .Setup(x => x.Keys(SplitKeyPrefix + "*"))
                .Returns(Array.Empty<RedisKey>());

            _redisAdapterMock
                .Setup(x => x.MGetAsync(It.IsAny<RedisKey[]>()))
                .ReturnsAsync(expectedResult);

            //Act
            var result = await _redisSplitCache.GetAllSplitsAsync();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task FetchMany_VerifyMGetCall_Once()
        {
            // Arrange.
            var splitNames = new List<string> { "Split_1", "Split_2", "Split_3" };

            _redisAdapterMock
                .Setup(mock => mock.MGetAsync(It.IsAny<RedisKey[]>()))
                .ReturnsAsync(new RedisValue[3]);

            // Act.
            var result = await _redisSplitCache.FetchManyAsync(splitNames);

            // Assert.
            _redisAdapterMock.Verify(mock => mock.MGetAsync(It.IsAny<RedisKey[]>()), Times.Once);
            _redisAdapterMock.Verify(mock => mock.GetAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GetNamesByFlagSetsAsync()
        {
            // Arrange.
            var flagSetNames = new List<string> { "set1", "set2", "set3", "set4" };

            _redisAdapterMock
                .Setup(mock => mock.PipelineSMembersAsync(It.Is<List<RedisKey>>(x => x.Count == 4)))
                .ReturnsAsync(new Dictionary<string, RedisValue[]>
                {
                    { "SPLITIO.flagSet.set1", new RedisValue[3] { "flag-name1" ,"flag-name2", "flag-name3" } },
                    { "SPLITIO.flagSet.set2", new RedisValue[3] { "flag-name1" ,"flag-name2", "flag-name3" } },
                    { "SPLITIO.flagSet.set3", new RedisValue[3] { "flag-name1" ,"flag-name2", "flag-name3" } },
                    { "SPLITIO.flagSet.set4", Array.Empty<RedisValue>() }
                });

            // Act.
            var result = await _redisSplitCache.GetNamesByFlagSetsAsync(flagSetNames);

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
                Name = splitName,
                ChangeNumber = 121291,
                Killed = false,
                Seed = 4324324,
                DefaultTreatment = "on",
                Conditions = new List<Splitio.Domain.Condition>(),
                Status = "ACTIVE",
                TrafficTypeName = "test"
            };
        }
    }
}
