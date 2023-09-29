using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisSegmentCacheAsyncTests
    {
        private const string SegmentKeyPrefix = "SPLITIO.segment.";

        private readonly Mock<IRedisAdapter> _redisAdapter;

        private readonly ISegmentCacheConsumer _cache;

        public RedisSegmentCacheAsyncTests()
        {
            _redisAdapter = new Mock<IRedisAdapter>();

            _cache = new RedisSegmentCache(_redisAdapter.Object);
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
