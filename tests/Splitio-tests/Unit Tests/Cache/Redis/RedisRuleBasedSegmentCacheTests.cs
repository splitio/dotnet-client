using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;

namespace Splitio_Tests.Unit_Tests.Cache.Redis
{
    [TestClass]
    public class RedisRuleBasedSegmentCacheTests
    {
        private Mock<IRedisAdapterConsumer> _redisAdapterMock;
        private RedisRuleBasedSegmentCache _cache;

        [TestInitialize]
        public void Initialize()
        {
            _redisAdapterMock = new Mock<IRedisAdapterConsumer>();
            var config = new RedisConfig();
            _cache = new RedisRuleBasedSegmentCache(_redisAdapterMock.Object, config, false);
        }

        [TestMethod]
        [Ignore("Until rbsParser is implemented")]
        public void Get_WhenSegmentExists_ReturnsSegment()
        {
            // Arrange
            var segmentName = "test-segment";
            var segmentJson = "{\"name\":\"test-segment\"}";
            _redisAdapterMock.Setup(x => x.Get(It.IsAny<string>())).Returns(segmentJson);

            // Act
            var result = _cache.Get(segmentName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(segmentName, result.Name);
        }

        [TestMethod]
        [Ignore("Until rbsParser is implemented")]
        public void Get_WhenSegmentDoesNotExist_ReturnsNull()
        {
            // Arrange
            var segmentName = "test-segment";
            _redisAdapterMock.Setup(x => x.Get(It.IsAny<string>())).Returns((string)null);

            // Act
            var result = _cache.Get(segmentName);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetChangeNumber_WhenValid_ReturnsChangeNumber()
        {
            // Arrange
            var changeNumber = "123";
            _redisAdapterMock.Setup(x => x.Get(It.IsAny<string>())).Returns(changeNumber);

            // Act
            var result = _cache.GetChangeNumber();

            // Assert
            Assert.AreEqual(123, result);
        }

        [TestMethod]
        public void GetChangeNumber_WhenInvalid_ReturnsMinusOne()
        {
            // Arrange
            var changeNumber = "invalid";
            _redisAdapterMock.Setup(x => x.Get(It.IsAny<string>())).Returns(changeNumber);

            // Act
            var result = _cache.GetChangeNumber();

            // Assert
            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        [Ignore("Until rbsParser is implemented")]
        public async Task GetAsync_WhenSegmentExists_ReturnsSegment()
        {
            // Arrange
            var segmentName = "test-segment";
            var segmentJson = "{\"name\":\"test-segment\"}";
            _redisAdapterMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(segmentJson);

            // Act
            var result = await _cache.GetAsync(segmentName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(segmentName, result.Name);
        }

        [TestMethod]
        [Ignore("Until rbsParser is implemented")]
        public async Task GetAsync_WhenSegmentDoesNotExist_ReturnsNull()
        {
            // Arrange
            var segmentName = "test-segment";
            _redisAdapterMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync((string)null);

            // Act
            var result = await _cache.GetAsync(segmentName);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetChangeNumberAsync_WhenValid_ReturnsChangeNumber()
        {
            // Arrange
            var changeNumber = "123";
            _redisAdapterMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(changeNumber);

            // Act
            var result = await _cache.GetChangeNumberAsync();

            // Assert
            Assert.AreEqual(123, result);
        }

        [TestMethod]
        public async Task GetChangeNumberAsync_WhenInvalid_ReturnsMinusOne()
        {
            // Arrange
            var changeNumber = "invalid";
            _redisAdapterMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(changeNumber);

            // Act
            var result = await _cache.GetChangeNumberAsync();

            // Assert
            Assert.AreEqual(-1, result);
        }
    }
}