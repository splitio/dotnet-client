using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisSegmentCacheTests
    {
        private const string segmentKeyPrefix = "SPLITIO.segment.";
        private const string segmentNameKeyPrefix = "SPLITIO.segment.{segmentname}.";
        private const string segmentsKeyPrefix = "SPLITIO.segments.";

        [TestMethod]
        public void GetRegisteredSegmentTest()
        {
            //Arrange
            var segmentName = "segment_test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            redisAdapterMock
                .Setup(x => x.SMembers(segmentsKeyPrefix + "registered"))
                .Returns(new RedisValue[] { segmentName });

            //Act
            var result = segmentCache.GetSegmentNames();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(segmentName, result[0]);
        }

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

        [TestMethod]
        public void GetChangeNumberWhenNotSetOrRedisExceptionTest()
        {
            //Arrange
            var changeNumber = -1;
            var segmentName = "segment_test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            redisAdapterMock
                .Setup(x => x.Get(segmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till"))
                .Returns("");

            //Act
            var result = segmentCache.GetChangeNumber(segmentName);

            //Assert
            Assert.AreEqual(changeNumber, result);
        }

        [TestMethod]
        public void GetSegmentNamesTest()
        {
            // Arrange.
            var redisAdapterMock = new Mock<IRedisAdapter>();
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);            

            // Act & Assert.
            var result = segmentCache.GetSegmentNames();
            Assert.AreEqual(0, result.Count);

            var segmentName = "test";

            redisAdapterMock
                .Setup(mock => mock.SMembers("SPLITIO.segments.registered"))
                .Returns(new List<RedisValue>
                {
                    segmentName,
                    $"{segmentName}-1",
                    $"{segmentName}-2"
                }.ToArray());

            result = segmentCache.GetSegmentNames();
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(segmentName));
            Assert.IsTrue(result.Contains($"{segmentName}-1"));
            Assert.IsTrue(result.Contains($"{segmentName}-2"));
        }
    }
}
