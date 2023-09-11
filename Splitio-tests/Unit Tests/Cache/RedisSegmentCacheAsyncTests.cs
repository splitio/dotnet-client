using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Cache.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisSegmentCacheAsyncTests
    {
        private const string SegmentKeyPrefix = "SPLITIO.segment.";
        private const string SegmentNameKeyPrefix = "SPLITIO.segment.{segmentname}.";
        private const string SegmentsKeyPrefix = "SPLITIO.segments.";

        private readonly Mock<IRedisAdapter> _redisAdapter;

        private readonly ISegmentCacheConsumer _cache;

        public RedisSegmentCacheAsyncTests()
        {
            _redisAdapter = new Mock<IRedisAdapter>();

            _cache = new RedisSegmentCache(_redisAdapter.Object);
        }

        [TestMethod]
        public async Task GetRegisteredSegmentTest()
        {
            // Arrange
            var segmentName = "segment_test";
            
            _redisAdapter
                .Setup(x => x.SMembersAsync(SegmentsKeyPrefix + "registered"))
                .ReturnsAsync(new RedisValue[] { segmentName });

            // Act
            var result = await _cache.GetSegmentNamesAsync();

            // Assert
            Assert.AreEqual(segmentName, result[0]);
        }

        [TestMethod]
        public async Task IsInSegmentAsyncTest()
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

        [TestMethod]
        public async Task GetSegmentKeysTest()
        {
            // Arrange.
            var keys = new List<string> { "abcd", "1234" };
            var segmentName = "test";

            _redisAdapter
                .Setup(mock => mock.SMembersAsync($"SPLITIO.segment.{segmentName}"))
                .ReturnsAsync(new List<RedisValue>{ "abcd", "1234" }.ToArray());

            // Act & Assert.
            var result = await _cache.GetSegmentKeysAsync(segmentName);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("abcd"));
            Assert.IsTrue(result.Contains("1234"));

            result = await _cache.GetSegmentKeysAsync("segmentName");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task GetSegmentNamesTest()
        {
            // Arrange.
            var segmentName = "test";

            _redisAdapter
               .Setup(mock => mock.SMembersAsync("SPLITIO.segments.registered"))
               .ReturnsAsync(new List<RedisValue> { segmentName, $"{segmentName}-1", $"{segmentName}-2" }.ToArray());

            // Act & Assert.
            var result = await _cache.GetSegmentNamesAsync();
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(segmentName));
            Assert.IsTrue(result.Contains($"{segmentName}-1"));
            Assert.IsTrue(result.Contains($"{segmentName}-2"));
        }
    }
}
