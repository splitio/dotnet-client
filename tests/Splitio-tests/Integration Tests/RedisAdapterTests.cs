using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Domain;
using Splitio.Tests.Common.Resources;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class RedisAdapterTests
    {
        private readonly string _userPrefix = "adapter-test";
        RedisAdapterForTests _adapter;
        RedisAdapterProducer _producer;

        [TestInitialize]
        public void Initialization()
        {
            var config = new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 1,
                RedisUserPrefix = _userPrefix
            };

            var pool = new ConnectionPoolManager(config);
            _adapter = new RedisAdapterForTests(config, pool);
            _producer = new RedisAdapterProducer(config, pool);

            CleanKeys();
        }

        [TestMethod]
        public void ExecuteSetAndGetSuccessful()
        {
            //Arrange
            var isSet = _adapter.Set($"{_userPrefix}-test_key", "test_value");

            //Act
            var result = _adapter.Get($"{_userPrefix}-test_key");

            //Assert
            Assert.IsTrue(isSet);
            Assert.AreEqual("test_value", result);

            var clusterAdapter = GetRedisClusterAdapter();
            isSet = clusterAdapter.Set($"{{SPLITIO}}{_userPrefix}-test_key", "test_value");
            result = clusterAdapter.Get($"{{SPLITIO}}{_userPrefix}-test_key");

            //Assert
            Assert.IsTrue(isSet);
            Assert.AreEqual("test_value", result);

        }

        [TestMethod]
        public void ExecuteSetShouldReturnFalseOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapterForTests(config, pool);

            //Act
            var isSet = adapter.Set($"{_userPrefix}-test_key", "test_value");

            //Assert
            Assert.IsFalse(isSet);
        }

        [TestMethod]
        public void ExecuteGetShouldReturnEmptyOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapterConsumer(config, pool);

            //Act
            var result = adapter.Get($"{_userPrefix}-test_key");

            //Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ExecuteMultipleSetAndMultipleGetSuccessful()
        {
            //Arrange
            var isSet1 = _adapter.Set($"{_userPrefix}-test_key", "test_value");
            var isSet2 = _adapter.Set($"{_userPrefix}-test_key2", "test_value2");
            var isSet3 = _adapter.Set($"{_userPrefix}-test_key3", "test_value3");

            //Act
            var result = _adapter.MGet(new RedisKey[]{$"{_userPrefix}-test_key", $"{_userPrefix}-test_key2", $"{_userPrefix}-test_key3" });

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(isSet1 & isSet2 & isSet3);
            Assert.AreEqual(3, result.Length);
            Assert.IsTrue(result.Contains("test_value"));
            Assert.IsTrue(result.Contains("test_value2"));
            Assert.IsTrue(result.Contains("test_value3"));

            var clusterAdapter = GetRedisClusterAdapter();
            isSet1 = clusterAdapter.Set($"{{SPLITIO}}{_userPrefix}-test_key", "test_value");
            isSet2 = clusterAdapter.Set($"{{SPLITIO}}{_userPrefix}-test_key2", "test_value2");
            isSet3 = clusterAdapter.Set($"{{SPLITIO}}{_userPrefix}-test_key3", "test_value3");

            //Act
            result = clusterAdapter.MGet(new RedisKey[] { $"{{SPLITIO}}{_userPrefix}-test_key", $"{_userPrefix}-test_key2", $"{_userPrefix}-test_key3" });

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(isSet1 & isSet2 & isSet3);
            Assert.AreEqual(3, result.Length);
            Assert.IsTrue(result.Contains("test_value"));
            Assert.IsTrue(result.Contains("test_value2"));
            Assert.IsTrue(result.Contains("test_value3"));

        }

        [TestMethod]
        public void ExecuteGetShouldReturnEmptyArrayOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapterConsumer(config, pool);

            //Act
            var result = adapter.MGet(new RedisKey[] { $"{_userPrefix}-test_key", $"{_userPrefix}-test_key2", $"{_userPrefix}-test_key3" });

            //Assert
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void ExecuteMultipleSetAndGetAllKeysWithFilterSuccessful()
        {
            //Arrange
            var isSet1 = _adapter.Set($"{_userPrefix}-test.test_key", "test_value");
            var isSet2 = _adapter.Set($"{_userPrefix}-test.test_key2", "test_value2");
            var isSet3 = _adapter.Set($"{_userPrefix}-test.test_key3", "test_value3");

            //Act
            var result = _adapter.Keys($"{_userPrefix}*");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(isSet1 & isSet2 & isSet3);
            Assert.AreEqual(3, result.Length);
            Assert.IsTrue(result.Contains($"{_userPrefix}-test.test_key"));
            Assert.IsTrue(result.Contains($"{_userPrefix}-test.test_key2"));
            Assert.IsTrue(result.Contains($"{_userPrefix}-test.test_key3"));

            var clusterAdapter = GetRedisClusterAdapter();
            isSet1 = clusterAdapter.Set($"{{SPLITIO}}{_userPrefix}-test.test_key", "test_value");
            isSet2 = clusterAdapter.Set($"{{SPLITIO}}{_userPrefix}-test.test_key2", "test_value2");
            isSet3 = clusterAdapter.Set($"{{SPLITIO}}{_userPrefix}-test.test_key3", "test_value3");

            //Act
            result = clusterAdapter.Keys($"{{SPLITIO}}{_userPrefix}*");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(isSet1 & isSet2 & isSet3);
            Assert.AreEqual(3, result.Length);
            Assert.IsTrue(result.Contains($"{{SPLITIO}}{_userPrefix}-test.test_key"));
            Assert.IsTrue(result.Contains($"{{SPLITIO}}{_userPrefix}-test.test_key2"));
            Assert.IsTrue(result.Contains($"{{SPLITIO}}{_userPrefix}-test.test_key3"));
        }

        [TestMethod]
        public void ExecuteKeysShouldReturnEmptyArrayOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapterConsumer(config, pool);

            //Act
            var result = adapter.Keys($"{_userPrefix}*");

            //Assert
            Assert.AreEqual(0, result.Length);
        }


        [TestMethod]
        public void ExecuteSetAndDelSuccessful()
        {
            //Arrange
            var isSet1 = _adapter.Set($"{_userPrefix}-testdel.test_key", "test_value");

            //Act
            var isDel = _adapter.Del(new RedisKey[] { $"{_userPrefix}-testdel.test_key" });
            var result = _adapter.Get($"{_userPrefix}-testdel.test_key");

            //Assert
            Assert.IsTrue(isSet1);
            Assert.AreEqual(1, isDel);
            Assert.IsNull(result);

            var clusterAdapter = GetRedisClusterAdapter();
            isSet1 = clusterAdapter.Set($"{{SPLITIO}}{_userPrefix}-testdel.test_key", "test_value");

            //Act
            isDel = clusterAdapter.Del(new RedisKey[] { $"{{SPLITIO}}{_userPrefix}-testdel.test_key" });
            result = clusterAdapter.Get($"{{SPLITIO}}{_userPrefix}-testdel.test_key");

            //Assert
            Assert.IsTrue(isSet1);
            Assert.AreEqual(1, isDel);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ExecuteDelShouldReturnFalseOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapterForTests(config, pool);

            //Act
            var isDel = adapter.Del(new RedisKey[] { $"{_userPrefix}-testdel.test_key" });

            //Assert
            Assert.AreNotEqual(1, isDel);
        }

        [TestMethod]
        public void ExecuteSetAndFlushSuccessful()
        {
            //Arrange
            var isSet1 = _adapter.Set($"{_userPrefix}-testflush.test_key", "test_value");

            //Act
            var result = _adapter.Keys($"{_userPrefix}*");
            _adapter.Del(result);
            result = _adapter.Keys($"{_userPrefix}*");

            //Assert
            Assert.IsTrue(isSet1);
            Assert.AreEqual(0, result.Length);

            var clusterAdapter = GetRedisClusterAdapter();
            isSet1 = clusterAdapter.Set($"{{SPLITIO}}{_userPrefix}-testflush.test_key", "test_value");

            //Act
            result = clusterAdapter.Keys($"{{SPLITIO}}{_userPrefix}*");
            clusterAdapter.Del(result);
            result = clusterAdapter.Keys($"{{SPLITIO}}{_userPrefix}*");

            //Assert
            Assert.IsTrue(isSet1);
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void ExecuteSAddAndSMemberSuccessful()
        {
            //Arrange
            var setCount = _adapter.SAdd($"{_userPrefix}-test_key_set", "test_value_1");

            //Act
            var result = _adapter.SMembers($"{_userPrefix}-test_key_set");

            //Assert
            Assert.AreEqual(true, setCount);
            Assert.AreEqual(1, result.Length);
            Assert.IsTrue(result.Contains("test_value_1"));
        }

        [TestMethod]
        public void ExecuteSAddShouldReturnFalseOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapterForTests(config, pool);

            //Act
            var setCount = adapter.SAdd($"{_userPrefix}-test_key_set", "test_value_1");

            //Assert
            Assert.IsFalse(setCount);
        }

        [TestMethod]
        public void ExecuteSMembersShouldReturnFalseOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapterConsumer(config, pool);

            //Act
            var result = adapter.SMembers($"{_userPrefix}-test_key_set");

            //Assert
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void ExecuteSAddAndSMembersSuccessful()
        {
            //Arrange
            var setCount = _adapter.SAdd($"{_userPrefix}-test_key_set_multiple", new RedisValue[]{ "test_value", "test_value2"});

            //Act
            var result = _adapter.SMembers($"{_userPrefix}-test_key_set_multiple");

            //Assert
            Assert.AreEqual(2, setCount);
            Assert.AreEqual(2, result.Length);
            Assert.IsTrue(result.Contains("test_value"));
            Assert.IsTrue(result.Contains("test_value2"));
        }

        [TestMethod]
        public void ExecuteSAddShouldReturnZeroOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapterForTests(config, pool);

            //Act
            var setCount = adapter.SAdd($"{_userPrefix}-test_key_set_multiple", new RedisValue[] { "test_value", "test_value2" });

            //Assert
            Assert.AreEqual(0, setCount);
        }

        [TestMethod]
        public void ExecuteSAddAndSRemSuccessful()
        {
            //Arrange
            _adapter.SAdd($"{_userPrefix}-test_key_set", new RedisValue[] { "test_value", "test_value2" });

            //Act
            _adapter.SRem($"{_userPrefix}-test_key_set", new RedisValue[] { "test_value2" });
            var result = _adapter.SIsMember($"{_userPrefix}-test_key_set", "test_value");
            var result2 = _adapter.SIsMember($"{_userPrefix}-test_key_set", "test_value2");
            var result3 = _adapter.SIsMember($"{_userPrefix}-test_key_set", "test_value3");
            
            //Assert
            Assert.IsTrue(result);
            Assert.IsFalse(result2);
            Assert.IsFalse(result3);

            var clusterAdapter = GetRedisClusterAdapter();
            clusterAdapter.SAdd($"{{SPLITIO}}{_userPrefix}-test_key_set", new RedisValue[] { "test_value", "test_value2" });

            //Act
            clusterAdapter.SRem($"{{SPLITIO}}{_userPrefix}-test_key_set", new RedisValue[] { "test_value2" });
            result = clusterAdapter.SIsMember($"{{SPLITIO}}{_userPrefix}-test_key_set", "test_value");
            result2 = clusterAdapter.SIsMember($"{{SPLITIO}}{_userPrefix}-test_key_set", "test_value2");
            result3 = clusterAdapter.SIsMember($"{{SPLITIO}}{_userPrefix}-test_key_set", "test_value3");

            //Assert
            Assert.IsTrue(result);
            Assert.IsFalse(result2);
            Assert.IsFalse(result3);
        }

        [TestMethod]
        public void ExecuteSRemShouldReturnZeroOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapterForTests(config, pool);

            //Act
            var remCount = adapter.SRem($"{_userPrefix}-test_key_set", new RedisValue[] { "test_value2" });

            //Assert
            Assert.AreEqual(0, remCount);
        }

        [TestMethod]
        public void ExecuteIncrBySuccessful()
        {
            //Arrange
            _adapter.IcrBy($"{_userPrefix}-test_count", 1);

            //Act
            var result = _adapter.IcrBy($"{_userPrefix}-test_count", 2);

            //Assert
            Assert.AreEqual(3, result);

            var clusterAdapter = GetRedisClusterAdapter();
            clusterAdapter.IcrBy($"{{SPLITIO}}{_userPrefix}-test_count", 1);

            //Act
            result = clusterAdapter.IcrBy($"{{SPLITIO}}{_userPrefix}-test_count", 2);

            //Assert
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void ExecuteIncrShouldReturnZeroOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapterForTests(config, pool);

            //Act
            var result = adapter.IcrBy($"{_userPrefix}-test_count", 2);

            //Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void ExecuteHashIncrementShouldReturnValue()
        {
            //Act & Assert
            var result = _producer.HashIncrement($"{_userPrefix}-test_count", "hashField", 2);
            Assert.AreEqual(2, result);

            result = _producer.HashIncrement($"{_userPrefix}-test_count", "hashField", 2);
            Assert.AreEqual(4, result);

            result = _producer.HashIncrement($"{_userPrefix}-test_count", "hashField", 3);
            Assert.AreEqual(7, result);

            result = _producer.HashIncrement($"{_userPrefix}-test", "hashField", 1);
            Assert.AreEqual(1, result);

            var clusterAdapter = GetRedisClusterAdapterProducer();
            //Act & Assert
            result = clusterAdapter.HashIncrement($"{{SPLITIO}}{_userPrefix}-test_count", "hashField", 2);
            Assert.AreEqual(2, result);

            result = clusterAdapter.HashIncrement($"{{SPLITIO}}{_userPrefix}-test_count", "hashField", 2);
            Assert.AreEqual(4, result);

            result = clusterAdapter.HashIncrement($"{{SPLITIO}}{_userPrefix}-test_count", "hashField", 3);
            Assert.AreEqual(7, result);

            result = clusterAdapter.HashIncrement($"{{SPLITIO}}{_userPrefix}-test", "hashField", 1);
            Assert.AreEqual(1, result);
        }

        private RedisAdapterForTests GetRedisClusterAdapter()
        {
            var config = new RedisConfig
            {
                ClusterNodes = new Splitio.Domain.ClusterNodes(new List<string>() { "localhost:6379" }, "{SPLITIO}"),
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 1,
                RedisUserPrefix = _userPrefix
            };

            var pool = new ConnectionPoolManager(config);
            return new RedisAdapterForTests(config, pool);
        }

        private RedisAdapterProducer GetRedisClusterAdapterProducer()
        {
            var config = new RedisConfig
            {
                ClusterNodes = new Splitio.Domain.ClusterNodes(new List<string>() { "localhost:6379" }, "{SPLITIO}"),
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 1,
                RedisUserPrefix = _userPrefix
            };

            var pool = new ConnectionPoolManager(config);
            return new RedisAdapterProducer(config, pool);
        }

        [TestCleanup]
        public void CleanKeys()
        {
            var keys = _adapter.Keys($"{_userPrefix}*");
            _adapter.Del(keys);

            keys = _adapter.Keys($"{{SPLITIO}}{_userPrefix}*");
            _adapter.Del(keys);
        }
    }
}
