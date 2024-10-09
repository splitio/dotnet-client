using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisSegmentCacheTests
    {
        private const string segmentKeyPrefix = "SPLITIO.segment.";

        [TestMethod]
        public void IsNotInSegmentOrRedisExceptionTest()
        {
            //Arrange
            var segmentName = "segment_test";
            var redisAdapterMock = new Mock<IRedisAdapterConsumer>();
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
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object, config, false);

            redisAdapterMock
                .Setup(x => x.SIsMember(segmentKeyPrefix + segmentName, "abcd"))
                .Returns(false);

            //Act
            var result = segmentCache.IsInSegment(segmentName, "abcd");

            //Assert
            Assert.IsFalse(result);
        }
        
        [TestMethod]
        public void IsInSegmentWithInexistentSegmentTest()
        {
            //Arrange
            var redisAdapterMock = new Mock<IRedisAdapterConsumer>();
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
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object, config, false);

            redisAdapterMock
                .Setup(x => x.SIsMember(segmentKeyPrefix + "test", "abcd"))
                .Returns(false);

            //Act
            var result = segmentCache.IsInSegment("test", "abcd");

            //Assert
            Assert.IsFalse(result);
        }
    }
}
