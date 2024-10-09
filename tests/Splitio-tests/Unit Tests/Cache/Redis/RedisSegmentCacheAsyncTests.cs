using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Cache.Interfaces;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisSegmentCacheAsyncTests
    {
        private const string SegmentKeyPrefix = "SPLITIO.segment.";

        private readonly Mock<IRedisAdapterConsumer> _redisAdapter;

        private readonly ISegmentCacheConsumer _cache;

        public RedisSegmentCacheAsyncTests()
        {
            _redisAdapter = new Mock<IRedisAdapterConsumer>();
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
            _cache = new RedisSegmentCache(_redisAdapter.Object, config, false);
        }

        [TestMethod]
        public async Task IsInSegmentAsyncTestFalse()
        {
            //Arrange
            var segmentName = "segment_test";

            _redisAdapter
                .Setup(x => x.SIsMemberAsync(SegmentKeyPrefix + segmentName, "abcd"))
                .ReturnsAsync(false);

            //Act
            var result = await _cache.IsInSegmentAsync(segmentName, "abcd");

            //Assert
            Assert.IsFalse(result);
        }
    }
}
