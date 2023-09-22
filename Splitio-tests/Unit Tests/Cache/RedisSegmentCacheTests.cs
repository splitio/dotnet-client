using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;

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
            var redisAdapterMock = new Mock<IRedisAdapter>();
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

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
            var redisAdapterMock = new Mock<IRedisAdapter>();
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

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
