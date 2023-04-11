using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Client.Classes;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Client.Classes;
using Splitio_Tests.Resources;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class RedisClientTests
    {
        private const string HOST = "localhost";
        private const string PORT = "6379";
        private const string PASSWORD = "";
        private const int DB = 0;
        private const string API_KEY = "redis_api_key";

        private ConfigurationOptions config;
        private IRedisAdapter _redisAdapter;

        [TestInitialize]
        public async Task Initialization()
        {
            var cacheAdapterConfig = new CacheAdapterConfigurationOptions
            {
                Host = HOST,
                Port = PORT,
                Password = PASSWORD,
                Database = DB
            };

            config = new ConfigurationOptions
            {
                CacheAdapterConfig = cacheAdapterConfig,
                SdkMachineIP = "192.168.0.1"
            };

            var rconfig = new RedisConfig
            {
                RedisHost = HOST,
                RedisPort = PORT,
                RedisPassword = PASSWORD,
                RedisDatabase = DB,
                PoolSize = 1
            };
            var pool = new ConnectionPoolManager(rconfig);
            _redisAdapter = new RedisAdapter(rconfig, pool);
            
            await LoadSplits();
        }

        [TestMethod]
        public void GetTreatment_WhenFeatureExists_ReturnsOn()
        {
            //Arrange
            var client = new RedisClient(config, API_KEY);

            client.BlockUntilReady(5000);

            //Act           
            var result = client.GetTreatment("test", "always_on", null);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("on", result);
        }

        [TestMethod]
        public void GetTreatment_WhenFeatureExists_ReturnsOff()
        {
            //Arrange
            var client = new RedisClient(config, API_KEY);

            client.BlockUntilReady(5000);

            //Act           
            var result = client.GetTreatment("test", "always_off", null);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("off", result);
        }

        [TestMethod]
        public void GetTreatment_WhenFeatureDoenstExist_ReturnsControl()
        {
            //Arrange
            var client = new RedisClient(config, API_KEY);
            client.BlockUntilReady(5000);

            //Act           
            var result = client.GetTreatment("test", "always_control", null);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("control", result);
        }

        [TestMethod]
        public void GetTreatments_WhenFeaturesExists_ReturnsOnOff()
        {
            //Arrange
            var alwaysOn = "always_on";
            var alwaysOff = "always_off";

            var features = new List<string> { alwaysOn, alwaysOff };

            var client = new RedisClient(config, API_KEY);

            client.BlockUntilReady(5000);

            //Act           
            var result = client.GetTreatments("test", features, null);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("off", result[alwaysOff]);
            Assert.AreEqual("on", result[alwaysOn]);
        }

        [TestMethod]
        public void GetTreatments_WhenOneFeatureDoenstExist_ReturnsOnOffControl()
        {
            //Arrange
            var alwaysOn = "always_on";
            var alwaysOff = "always_off";
            var alwaysControl = "always_control";

            var features = new List<string> { alwaysOn, alwaysOff, alwaysControl };

            var client = new RedisClient(config, API_KEY);

            client.BlockUntilReady(5000);

            //Act           
            var result = client.GetTreatments("test", features, null);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("off", result[alwaysOff]);
            Assert.AreEqual("on", result[alwaysOn]);
            Assert.AreEqual("control", result[alwaysControl]);
        }

        [TestMethod]
        public void GetTreatmentsWithConfig_WhenClientIsNotReady_ReturnsControl()
        {
            // Arrange.
            var client = new RedisClient(config, API_KEY);
            
            // Act.
            var result = client.GetTreatmentsWithConfig("key", new List<string>());

            // Assert.
            foreach (var res in result)
            {
                Assert.AreEqual("control", res.Value.Treatment);
                Assert.IsNull(res.Value.Config);
            }
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WhenClientIsNotReady_ReturnsControl()
        {
            // Arrange.
            var client = new RedisClient(config, API_KEY);

            // Act.
            var result = client.GetTreatmentWithConfig("key", string.Empty);

            // Assert.
            Assert.AreEqual("control", result.Treatment);
            Assert.IsNull(result.Config);
        }

        [TestMethod]
        public void GetTreatment_WhenClientIsNotReady_ReturnsControl()
        {
            // Arrange.
            var client = new RedisClient(config, API_KEY);

            // Act.
            var result = client.GetTreatment("key", string.Empty);

            // Assert.
            Assert.AreEqual("control", result);
        }

        [TestMethod]
        public void GetTreatments_WhenClientIsNotReady_ReturnsControl()
        {
            // Arrange.
            config.CacheAdapterConfig.Host = "fake-host";
            var client = new RedisClient(config, API_KEY);

            // Act.
            var result = client.GetTreatments("key", new List<string>());

            // Assert.
            foreach (var res in result)
            {
                Assert.AreEqual("control", res.Value);
            }
        }

        [TestMethod]
        public void Track_WhenClientIsNotReady_ReturnsTrue()
        {
            // Arrange.
            var client = new RedisClient(config, API_KEY);

            // Act.
            var result = client.Track("key", "traffic_type", "event_type");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Destroy()
        {
            //Arrange
            var client = new RedisClient(config, API_KEY);
            client.BlockUntilReady(5000);

            //Act
            client.Destroy();

            //Assert
            Assert.IsTrue(client.IsDestroyed());
        }

        private async Task LoadSplits()
        {
            _redisAdapter.Flush();

            await _redisAdapter.SetAsync("SPLITIO.split.always_on", SplitsHelper.AlwaysOn);
            await _redisAdapter.SetAsync("SPLITIO.split.always_off", SplitsHelper.AlwaysOff);
        }
    }
}
