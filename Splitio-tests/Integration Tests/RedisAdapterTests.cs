using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using StackExchange.Redis;
using System;
using System.Linq;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class RedisAdapterTests
    {
        IRedisAdapter _adapter;

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
            };

            var pool = new ConnectionPoolManager(config);
            _adapter = new RedisAdapter(config, pool);
            _adapter.Flush();
        }

        [TestMethod]
        public void ExecuteSetAndGetSuccessful()
        {
            //Arrange
            var isSet = _adapter.Set("test_key", "test_value");

            //Act
            var result = _adapter.Get("test_key");

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

            var adapter = new RedisAdapter(config, pool);

            //Act
            var isSet = adapter.Set("test_key", "test_value");

            //Assert
            Assert.IsFalse(isSet);
        }

        [TestMethod]
        public void ExecuteGetShouldReturnEmptyOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var result = adapter.Get("test_key");

            //Assert
            Assert.AreEqual(String.Empty, result);
        }

        [TestMethod]
        public void ExecuteMultipleSetAndMultipleGetSuccessful()
        {
            //Arrange
            var isSet1 = _adapter.Set("test_key", "test_value");
            var isSet2 = _adapter.Set("test_key2", "test_value2");
            var isSet3 = _adapter.Set("test_key3", "test_value3");

            //Act
            var result = _adapter.MGet(new RedisKey[]{"test_key", "test_key2", "test_key3"});

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

            var adapter = new RedisAdapter(config, pool);

            //Act
            var result = adapter.MGet(new RedisKey[] { "test_key", "test_key2", "test_key3" });

            //Assert
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void ExecuteMultipleSetAndGetAllKeysWithFilterSuccessful()
        {
            //Arrange
            var isSet1 = _adapter.Set("test.test_key", "test_value");
            var isSet2 = _adapter.Set("test.test_key2", "test_value2");
            var isSet3 = _adapter.Set("test.test_key3", "test_value3");

            //Act
            var result = _adapter.Keys("test.*");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(isSet1 & isSet2 & isSet3);
            Assert.AreEqual(3, result.Length);
            Assert.IsTrue(result.Contains("test.test_key"));
            Assert.IsTrue(result.Contains("test.test_key2"));
            Assert.IsTrue(result.Contains("test.test_key3"));
        }

        [TestMethod]
        public void ExecuteKeysShouldReturnEmptyArrayOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var result = adapter.Keys("test.*");

            //Assert
            Assert.AreEqual(0, result.Length);
        }


        [TestMethod]
        public void ExecuteSetAndDelSuccessful()
        {
            //Arrange
            var isSet1 = _adapter.Set("testdel.test_key", "test_value");

            //Act
            var isDel = _adapter.Del("testdel.test_key");
            var result = _adapter.Get("testdel.test_key");

            //Assert
            Assert.IsTrue(isSet1);
            Assert.IsTrue(isDel);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ExecuteDelShouldReturnFalseOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var isDel = adapter.Del("testdel.test_key");

            //Assert
            Assert.IsFalse(isDel);
        }

        [TestMethod]
        public void ExecuteSetAndFlushSuccessful()
        {
            //Arrange
            var isSet1 = _adapter.Set("testflush.test_key", "test_value");

            //Act
            _adapter.Flush();
            var result = _adapter.Keys("test.*");

            //Assert
            Assert.IsTrue(isSet1);
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void ExecuteSAddAndSMemberSuccessful()
        {
            //Arrange
            var setCount = _adapter.SAdd("test_key_set", "test_value_1");

            //Act
            var result = _adapter.SMembers("test_key_set");

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

            var adapter = new RedisAdapter(config, pool);

            //Act
            var setCount = adapter.SAdd("test_key_set", "test_value_1");

            //Assert
            Assert.IsFalse(setCount);
        }

        [TestMethod]
        public void ExecuteSMembersShouldReturnFalseOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var result = adapter.SMembers("test_key_set");

            //Assert
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void ExecuteSAddAndSMembersSuccessful()
        {
            //Arrange
            var setCount = _adapter.SAdd("test_key_set_multiple", new RedisValue[]{ "test_value", "test_value2"});

            //Act
            var result = _adapter.SMembers("test_key_set_multiple");

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

            var adapter = new RedisAdapter(config, pool);

            //Act
            var setCount = adapter.SAdd("test_key_set_multiple", new RedisValue[] { "test_value", "test_value2" });

            //Assert
            Assert.AreEqual(0, setCount);
        }

        [TestMethod]
        public void ExecuteSAddAndSRemSuccessful()
        {
            //Arrange
            var setCount = _adapter.SAdd("test_key_set", new RedisValue[] { "test_value", "test_value2" });

            //Act
            var remCount = _adapter.SRem("test_key_set", new RedisValue[] { "test_value2" });
            var result = _adapter.SIsMember("test_key_set", "test_value");
            var result2 = _adapter.SIsMember("test_key_set", "test_value2");
            var result3 = _adapter.SIsMember("test_key_set", "test_value3");
            
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

            var adapter = new RedisAdapter(config, pool);

            //Act
            var remCount = adapter.SRem("test_key_set", new RedisValue[] { "test_value2" });

            //Assert
            Assert.AreEqual(0, remCount);
        }

        [TestMethod]
        public void ExecuteIncrBySuccessful()
        {
            //Arrange
            _adapter.IcrBy("test_count", 1);

            //Act
            var result = _adapter.IcrBy("test_count", 2);

            //Assert
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void ExecuteIncrShouldReturnZeroOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var result = adapter.IcrBy("test_count", 2);

            //Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void ExecuteHashIncrementShouldReturnValue()
        {
            //Act & Assert
            var result = _adapter.HashIncrement("test_count", "hashField", 2);
            Assert.AreEqual(2, result);

            result = _adapter.HashIncrement("test_count", "hashField", 2);
            Assert.AreEqual(4, result);

            result = _adapter.HashIncrement("test_count", "hashField", 3);
            Assert.AreEqual(7, result);

            result = _adapter.HashIncrement("test", "hashField", 1);
            Assert.AreEqual(1, result);
        }
    }
}
