using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisSegmentCacheTests
    {
        private const string segmentKeyPrefix = "SPLITIO.segment.";
        private const string segmentNameKeyPrefix = "SPLITIO.segment.{segmentname}.";
        private const string segmentsKeyPrefix = "SPLITIO.segments.";

        [TestMethod]
        public async Task RegisterSegmentTest()
        {
            //Arrange
            var segmentName = "test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.SAddAsync(It.IsAny<string>(), It.IsAny<RedisValue[]>())).ReturnsAsync(1);
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);
            
            //Act
            var result = await segmentCache.RegisterSegmentsAsync(new List<string> { segmentName });

            //Assert
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public async Task AddToSegmentTest()
        {
            //Arrange
            var segmentName = "test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.SAddAsync(It.IsAny<string>(), It.IsAny<RedisValue[]>())).ReturnsAsync(1);
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            await segmentCache.AddToSegmentAsync(segmentName, new List<string>() { "test" });

            //Assert
            redisAdapterMock.Verify(mock => mock.SAddAsync(segmentKeyPrefix + segmentName, It.IsAny<RedisValue[]>()));
        }

        [TestMethod]
        public async Task GetRegisteredSegmentTest()
        {
            //Arrange
            var segmentName = "segment_test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.SMembersAsync(segmentsKeyPrefix + "registered")).ReturnsAsync(new RedisValue[]{segmentName});
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            var result = await segmentCache.GetSegmentNamesAsync();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(segmentName, result[0]);
        }

        [TestMethod]
        public async Task IsNotInSegmentOrRedisExceptionTest()
        {
            //Arrange
            var segmentName = "segment_test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.SIsMemberAsync(segmentKeyPrefix + segmentName, "abcd")).ReturnsAsync(false);

            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            var result = await segmentCache.IsInSegmentAsync(segmentName, "abcd");

            //Assert
            Assert.IsFalse(result);
        }
        
        [TestMethod]
        public async Task IsInSegmentWithInexistentSegmentTest()
        {
            //Arrange
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.SIsMemberAsync(segmentKeyPrefix + "test", "abcd")).ReturnsAsync(false);

            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            var result = await segmentCache.IsInSegmentAsync("test", "abcd");

            //Assert
            Assert.IsFalse(result);
        }
 
        [TestMethod]
        public async Task RemoveKeyFromSegmentTest()
        {
            //Arrange
            var segmentName = "segment_test";
            var keys = new List<string> { "abcd" };
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.SRemAsync(segmentKeyPrefix + segmentName, It.IsAny<RedisValue[]>())).ReturnsAsync(1);
            redisAdapterMock.Setup(x => x.SIsMemberAsync(segmentKeyPrefix + segmentName, "abcd")).ReturnsAsync(false);

            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            await segmentCache.RemoveFromSegmentAsync(segmentName, keys);
            var result = await segmentCache.IsInSegmentAsync(segmentName, keys.First());

            //Assert
            Assert.IsFalse(result);
        }
        

        [TestMethod]
        public async Task SetAndGetChangeNumberTest()
        {
            //Arrange
            var changeNumber = 1234;
            var segmentName = "segment_test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.SetAsync(segmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till", changeNumber.ToString())).ReturnsAsync(true);
            redisAdapterMock.Setup(x => x.GetAsync(segmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till")).ReturnsAsync(changeNumber.ToString());
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            await segmentCache.SetChangeNumberAsync(segmentName, 1234);
            var result = await segmentCache.GetChangeNumberAsync(segmentName);

            //Assert
            Assert.AreEqual(1234, result);
        }

        [TestMethod]
        public async Task GetChangeNumberWhenNotSetOrRedisExceptionTest()
        {
            //Arrange
            var changeNumber = -1;
            var segmentName = "segment_test";
            var redisAdapterMock = new Mock<IRedisAdapter>();
            redisAdapterMock.Setup(x => x.GetAsync(segmentNameKeyPrefix.Replace("{segmentname}", segmentName) + "till")).ReturnsAsync("");
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            var result = await segmentCache.GetChangeNumberAsync(segmentName);

            //Assert
            Assert.AreEqual(changeNumber, result);
        }

        [TestMethod]
        public void FlushTest()
        {
            //Arrange
            var redisAdapterMock = new Mock<IRedisAdapter>();
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            //Act
            segmentCache.Flush();

            //Assert
            redisAdapterMock.Verify(mock => mock.Flush(), Times.Once());
        }

        [TestMethod]
        public async Task GetSegmentKeysTest()
        {
            // Arrange.
            var redisAdapterMock = new Mock<IRedisAdapter>();
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);

            var keys = new List<string> { "abcd", "1234" };
            var segmentName = "test";
            await segmentCache.AddToSegmentAsync(segmentName, keys);

            redisAdapterMock
                .Setup(mock => mock.SMembersAsync($"SPLITIO.segment.{segmentName}"))
                .ReturnsAsync(new List<RedisValue>
                {
                    "abcd",
                    "1234"
                }.ToArray());

            // Act & Assert.
            var result = await segmentCache.GetSegmentKeysAsync(segmentName);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("abcd"));
            Assert.IsTrue(result.Contains("1234"));

            result = await segmentCache.GetSegmentKeysAsync("segmentName");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task GetSegmentNamesTest()
        {
            // Arrange.
            var redisAdapterMock = new Mock<IRedisAdapter>();
            var segmentCache = new RedisSegmentCache(redisAdapterMock.Object);            

            // Act & Assert.
            var result = await segmentCache.GetSegmentNamesAsync();
            Assert.AreEqual(0, result.Count);

            var segmentName = "test";

            redisAdapterMock
                .Setup(mock => mock.SMembersAsync("SPLITIO.segments.registered"))
                .ReturnsAsync(new List<RedisValue>
                {
                    segmentName,
                    $"{segmentName}-1",
                    $"{segmentName}-2"
                }.ToArray());

            result = await segmentCache.GetSegmentNamesAsync();
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(segmentName));
            Assert.IsTrue(result.Contains($"{segmentName}-1"));
            Assert.IsTrue(result.Contains($"{segmentName}-2"));
        }
    }
}
