using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache.Redis
{
    [TestClass]
    public class RedisEventsCacheTests
    {
        private readonly Mock<IRedisAdapterProducer> _redisAdapterProducer;

        private readonly RedisEventsCache _redisEventsCache;

        public RedisEventsCacheTests()
        {
            _redisAdapterProducer = new Mock<IRedisAdapterProducer>();
            var config = new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                RedisUserPrefix = "prefix",
                PoolSize = 1,
            };
            _redisEventsCache = new RedisEventsCache(_redisAdapterProducer.Object, "machine-name", "machine-ip", "sdk-version", config);
        }

        [TestMethod]
        public void AddItemsWithEmptyList()
        {
            // Act.
            var result = _redisEventsCache.AddItems(new List<WrappedEvent>());

            // Assert.
            Assert.AreEqual(0, result);
            _redisAdapterProducer.Verify(mock => mock.ListRightPush(It.IsAny<string>(), It.IsAny<RedisValue>()), Times.Never);
        }

        [TestMethod]
        public void AddItemsWithItems()
        {
            // Arrange.
            var events = GetEvents();
            var key = "prefix.SPLITIO.events";
            var value = "{\"m\":{\"s\":\"sdk-version\",\"i\":\"machine-ip\",\"n\":\"machine-name\"},\"e\":{\"key\":\"key\",\"trafficTypeName\":\"trafficTypeName\",\"eventTypeId\":\"eventTypeId\",\"value\":123.0,\"timestamp\":0,\"properties\":null}}";

            _redisAdapterProducer
                .Setup(mock => mock.ListRightPush(key, value))
                .Returns(1);

            // Act.
            var result = _redisEventsCache.AddItems(events);

            // Assert.
            Assert.AreEqual(1, result);
            _redisAdapterProducer.Verify(mock => mock.ListRightPush(key, value), Times.Once);
        }

        [TestMethod]
        public async Task AddItemsAsyncWithEmptyList()
        {
            // Act.
            var result = await _redisEventsCache.AddItemsAsync(new List<WrappedEvent>());

            // Assert.
            Assert.AreEqual(0, result);
            _redisAdapterProducer.Verify(mock => mock.ListRightPushAsync(It.IsAny<string>(), It.IsAny<RedisValue>()), Times.Never);
        }

        [TestMethod]
        public async Task AddItemsAsyncWithItems()
        {
            // Arrange.
            var events = GetEvents();
            var key = "prefix.SPLITIO.events";
            var value = "{\"m\":{\"s\":\"sdk-version\",\"i\":\"machine-ip\",\"n\":\"machine-name\"},\"e\":{\"key\":\"key\",\"trafficTypeName\":\"trafficTypeName\",\"eventTypeId\":\"eventTypeId\",\"value\":123.0,\"timestamp\":0,\"properties\":null}}";

            _redisAdapterProducer
                .Setup(mock => mock.ListRightPushAsync(key, value))
                .ReturnsAsync(1);

            // Act.
            var result = await _redisEventsCache.AddItemsAsync(events);

            // Assert.
            Assert.AreEqual(1, result);
            _redisAdapterProducer.Verify(mock => mock.ListRightPushAsync(key, value), Times.Once);
        }

        private List<WrappedEvent> GetEvents()
        {
            return new List<WrappedEvent>
            {
                new WrappedEvent
                {
                    Event = new Event
                    {
                        key = "key",
                        value = 123,
                        eventTypeId = "eventTypeId",
                        trafficTypeName = "trafficTypeName"
                    }
                }
            };
        }
    }
}
