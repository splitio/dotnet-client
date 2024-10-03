using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Client.Classes;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Tests.Common.Resources;
using Splitio_Tests.Resources;
using System.Collections.Generic;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class RedisClientTests
    {
        private readonly string _prefix = "SPLITIO-TEST";

        private const string HOST = "localhost";
        private const string PORT = "6379";
        private const string PASSWORD = "";
        private const int DB = 0;
        private const string API_KEY = "redis_api_key";

        private ConfigurationOptions config;
        private RedisAdapterForTests _redisAdapter;

        [TestInitialize]
        public void Initialization()
        {
            var cacheAdapterConfig = new CacheAdapterConfigurationOptions
            {
                Host = HOST,
                Port = PORT,
                Password = PASSWORD,
                Database = DB,
                UserPrefix = _prefix
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
                PoolSize = 1,
                RedisUserPrefix = _prefix
            };
            var pool = new ConnectionPoolManager(rconfig);
            _redisAdapter = new RedisAdapterForTests(rconfig, pool);

            CleanKeys();
            LoadSplits();
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

            var client2 = new RedisClient(GetRedisClusterConfigurationOptions(), API_KEY);

            client2.BlockUntilReady(5000);

            //Act           
            result = client2.GetTreatment("test", "always_on", null);

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

            var client2 = new RedisClient(GetRedisClusterConfigurationOptions(), API_KEY);

            client2.BlockUntilReady(5000);
            result = client2.GetTreatment("test", "always_off", null);

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

            var client2 = new RedisClient(GetRedisClusterConfigurationOptions(), API_KEY);

            client2.BlockUntilReady(5000);
            result = client2.GetTreatment("test", "always_control", null);

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


            var client2 = new RedisClient(GetRedisClusterConfigurationOptions(),
                API_KEY);

            client2.BlockUntilReady(5000);
            result = client2.GetTreatments("test", features, null);

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

            var client2 = new RedisClient(GetRedisClusterConfigurationOptions(),
    API_KEY);

            client2.BlockUntilReady(5000);
            result = client2.GetTreatments("test", features, null);

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

            var client2 = new RedisClient(GetRedisClusterConfigurationOptions(),
API_KEY);

            client2.BlockUntilReady(5000);
            result = client2.GetTreatmentsWithConfig("key", new List<string>());

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

            var client2 = new RedisClient(GetRedisClusterConfigurationOptions(),
API_KEY);

            client2.BlockUntilReady(5000);
            result = client2.GetTreatmentWithConfig("key", string.Empty);

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

            var client2 = new RedisClient(GetRedisClusterConfigurationOptions(),
API_KEY);

            client2.BlockUntilReady(5000);
            result = client2.GetTreatment("key", string.Empty);

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

            var client2 = new RedisClient(GetRedisClusterConfigurationOptions(), API_KEY);

            client2.BlockUntilReady(5000);
            result = client2.GetTreatments("key", new List<string>());

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

            var client2 = new RedisClient(GetRedisClusterConfigurationOptions(),
API_KEY);

            client2.BlockUntilReady(5000);
            // Act.
            result = client2.Track("key", "traffic_type", "event_type");
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

        private ConfigurationOptions GetRedisClusterConfigurationOptions()
        {
            var clusterNodes = new ClusterNodes(
                new List<string>()
                {
                    HOST + ":" + PORT
                },
                "{{SPLITIO}}");
            var cacheAdapterConfig = new CacheAdapterConfigurationOptions
            {
                Host = HOST,
                Port = PORT,
                Password = PASSWORD,
                Database = DB,
                UserPrefix = _prefix
            };

            return new ConfigurationOptions
            {
                CacheAdapterConfig = cacheAdapterConfig,
                SdkMachineIP = "192.168.0.1"
            };
        }

    [TestCleanup]
        public void CleanKeys()
        {
            var keys = _redisAdapter.Keys($"{_prefix}*");
            _redisAdapter.Del(keys);

            keys = _redisAdapter.Keys($"{{SPLITIO}}{_prefix}*");
            _redisAdapter.Del(keys);

        }

        private void LoadSplits()
        {
            _redisAdapter.Set($"{_prefix}.SPLITIO.split.always_on", SplitsHelper.AlwaysOn);
            _redisAdapter.Set($"{_prefix}.SPLITIO.split.always_off", SplitsHelper.AlwaysOff);

            _redisAdapter.Set($"{{SPLITIO}}{_prefix}.SPLITIO.split.always_on", SplitsHelper.AlwaysOn);
            _redisAdapter.Set($"{{SPLITIO}}{_prefix}.SPLITIO.split.always_off", SplitsHelper.AlwaysOff);
        }
    }
}
