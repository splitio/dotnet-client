using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Parsing.Interfaces;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache.Redis
{
    [TestClass]
    public class RedisRuleBasedSegmentCacheTests
    {
        private Mock<IRedisAdapterConsumer> _redisAdapterMock;
        private Mock<IParser<RuleBasedSegmentDto, RuleBasedSegment>> _rbsParser;
        private RedisRuleBasedSegmentCache _cache;

        [TestInitialize]
        public void Initialize()
        {
            _redisAdapterMock = new Mock<IRedisAdapterConsumer>();
            _rbsParser = new Mock<IParser<RuleBasedSegmentDto, RuleBasedSegment>>();
            var config = new RedisConfig();
            
            _cache = new RedisRuleBasedSegmentCache(_redisAdapterMock.Object, _rbsParser.Object, config, false);
        }

        [TestMethod]
        public void Get_WhenSegmentExists_ReturnsSegment()
        {
            // Arrange
            var segmentName = "test-segment";
            var segmentJson = "{\"name\":\"test-segment\"}";
            _redisAdapterMock
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns(segmentJson);

            _rbsParser
                .Setup(x => x.Parse(It.IsAny<RuleBasedSegmentDto>()))
                .Returns(new RuleBasedSegment
                {
                    Name = segmentName,
                });
                

            // Act
            var result = _cache.Get(segmentName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(segmentName, result.Name);
        }

        [TestMethod]
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
        public async Task GetAsync_WhenSegmentExists_ReturnsSegment()
        {
            // Arrange
            var segmentName = "test-segment";
            var segmentJson = "{\"name\":\"test-segment\"}";
            _redisAdapterMock
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(segmentJson);

            _rbsParser
                .Setup(x => x.Parse(It.IsAny<RuleBasedSegmentDto>()))
                .Returns(new RuleBasedSegment
                {
                    Name = segmentName,
                });

            // Act
            var result = await _cache.GetAsync(segmentName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(segmentName, result.Name);
        }

        [TestMethod]
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