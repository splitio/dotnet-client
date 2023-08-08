using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisSegmentCacheTests
    {
        private const string segmentKeyPrefix = "SPLITIO.segment.";
        private const string segmentNameKeyPrefix = "SPLITIO.segment.{segmentname}.";
        private const string segmentsKeyPrefix = "SPLITIO.segments.";

        [TestMethod]
        public void RegisterSegmentTest()
        {
            //Arrange
            var segmentName = "test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.SAdd(It.IsAny<string>(), It.IsAny<RedisValue[]>())).Returns(1);
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);
            
            //Act
            var result = segmentCache.RegisterSegment(segmentName);

            //Assert
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void AddToSegmentTest()
        {
            //Arrange
            var segmentName = "test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.SAdd(It.IsAny<string>(), It.IsAny<RedisValue[]>())).Returns(1);
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            segmentCache.AddToSegment(segmentName, new List<string>() { "test" });

            //Assert
            redisAdapterMock.Verify(mock => mock.SAdd(segmentKeyPrefix + segmentName, It.IsAny<RedisValue[]>()));
        }

        [TestMethod]
        public void GetRegisteredSegmentTest()
        {
            //Arrange
            var segmentName = "segment_test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.SMembers(segmentsKeyPrefix + "registered")).Returns(new RedisValue[]{segmentName});
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

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
            redisAdapterMock.Setup(x => x.SIsMember(segmentKeyPrefix + segmentName, "abcd")).Returns(false);

            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

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
            redisAdapterMock.Setup(x => x.SIsMember(segmentKeyPrefix + "test", "abcd")).Returns(false);

            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            var result = segmentCache.IsInSegment("test", "abcd");

            //Assert
            Assert.IsFalse(result);
        }
 
        [TestMethod]
        public void RemoveKeyFromSegmentTest()
        {
            //Arrange
            var segmentName = "segment_test";
            var keys = new List<string> { "abcd" };
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.SRem(segmentKeyPrefix + segmentName, It.IsAny<RedisValue[]>())).Returns(1);
            redisAdapterMock.Setup(x => x.SIsMember(segmentKeyPrefix + segmentName, "abcd")).Returns(false);

            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            segmentCache.RemoveFromSegment(segmentName, keys);
            var result = segmentCache.IsInSegment(segmentName, keys.First());

            //Assert
            Assert.IsFalse(result);
        }
        

        [TestMethod]
        public void SetAndGetChangeNumberTest()
        {
            //Arrange
            var changeNumber = 1234;
            var segmentName = "segment_test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.Set(segmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till", changeNumber.ToString())).Returns(true);
            redisAdapterMock.Setup(x => x.Get(segmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till")).Returns(changeNumber.ToString());
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            segmentCache.SetChangeNumber(segmentName, 1234);
            var result = segmentCache.GetChangeNumber(segmentName);

            //Assert
            Assert.AreEqual(1234, result);
        }

        [TestMethod]
        public void GetChangeNumberWhenNotSetOrRedisExceptionTest()
        {
            //Arrange
            var changeNumber = -1;
            var segmentName = "segment_test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.Get(segmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till")).Returns("");
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            var result = segmentCache.GetChangeNumber(segmentName);

            //Assert
            Assert.AreEqual(changeNumber, result);
        }

        [TestMethod]
        public void GetSegmentKeysTest()
        {
            // Arrange.
            var redisAdapterMock = new Mock<IRedisAdapter>();
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            var keys = new List<string> { "abcd", "1234" };
            var segmentName = "test";
            segmentCache.AddToSegment(segmentName, keys);

            redisAdapterMock
                .Setup(mock => mock.SMembers($"SPLITIO.segment.{segmentName}"))
                .Returns(new List<RedisValue>
                {
                    "abcd",
                    "1234"
                }.ToArray());

            // Act & Assert.
            var result = segmentCache.GetSegmentKeys(segmentName);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("abcd"));
            Assert.IsTrue(result.Contains("1234"));

            result = segmentCache.GetSegmentKeys("segmentName");
            Assert.AreEqual(0, result.Count);
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
