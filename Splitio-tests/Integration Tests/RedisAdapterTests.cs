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
        IRedisAdapter adapter;

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
            adapter = new RedisAdapter(config, pool);
            adapter.Flush();
        }

        [TestMethod]
        public async void ExecuteSetAndGetSuccessful()
        {
            //Arrange
            var isSet = await adapter.SetAsync("test_key", "test_value");

            //Act
            var result = await adapter.GetAsync("test_key");

            //Assert
            Assert.IsTrue(isSet);
            Assert.AreEqual("test_value", result);
        }

        [TestMethod]
        public async void ExecuteSetShouldReturnFalseOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var isSet = await adapter.SetAsync("test_key", "test_value");

            //Assert
            Assert.IsFalse(isSet);
        }

        [TestMethod]
        public async void ExecuteGetShouldReturnEmptyOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var result = await adapter.GetAsync("test_key");

            //Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public async void ExecuteMultipleSetAndMultipleGetSuccessful()
        {
            //Arrange
            var isSet1 = await adapter.SetAsync("test_key", "test_value");
            var isSet2 = await adapter.SetAsync("test_key2", "test_value2");
            var isSet3 = await adapter.SetAsync("test_key3", "test_value3");

            //Act
            var result = await adapter.MGetAsync(new RedisKey[]{"test_key", "test_key2", "test_key3"});

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(isSet1 & isSet2 & isSet3);
            Assert.AreEqual(3, result.Length);
            Assert.IsTrue(result.Contains("test_value"));
            Assert.IsTrue(result.Contains("test_value2"));
            Assert.IsTrue(result.Contains("test_value3"));
        }

        [TestMethod]
        public async void ExecuteGetShouldReturnEmptyArrayOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var result = await adapter.MGetAsync(new RedisKey[] { "test_key", "test_key2", "test_key3" });

            //Assert
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public async void ExecuteMultipleSetAndGetAllKeysWithFilterSuccessful()
        {
            //Arrange
            var isSet1 = await adapter.SetAsync("test.test_key", "test_value");
            var isSet2 = await adapter.SetAsync("test.test_key2", "test_value2");
            var isSet3 = await adapter.SetAsync("test.test_key3", "test_value3");

            //Act
            var result = adapter.Keys("test.*");

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
        public async void ExecuteSetAndDelSuccessful()
        {
            //Arrange
            var isSet1 = await adapter.SetAsync("testdel.test_key", "test_value");

            //Act
            var countDel = adapter.Del(new RedisKey[] { "testdel.test_key" });
            var result = await adapter.GetAsync("testdel.test_key");

            //Assert
            Assert.IsTrue(isSet1);
            Assert.AreEqual(1, countDel);
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
            var countDel = adapter.Del(new RedisKey[] { "testdel.test_key" });

            //Assert
            Assert.AreEqual(0, countDel);
        }

        [TestMethod]
        public async void ExecuteSetAndFlushSuccessful()
        {
            //Arrange
            var isSet1 = await adapter.SetAsync("testflush.test_key", "test_value");

            //Act
            adapter.Flush();
            var result = adapter.Keys("test.*");

            //Assert
            Assert.IsTrue(isSet1);
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public async void ExecuteSAddAndSMemberSuccessful()
        {
            //Arrange
            var setCount = await adapter.SetAsync("test_key_set", "test_value_1");

            //Act
            var result = await adapter.GetAsync("test_key_set");

            //Assert
            Assert.AreEqual(true, setCount);
            Assert.AreEqual(1, result.Length);
            Assert.IsTrue(result.Contains("test_value_1"));
        }

        [TestMethod]
        public async void ExecuteSAddShouldReturnFalseOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var setCount = await adapter.SAddAsync("test_key_set", new RedisValue[] { "test_value_1" });

            //Assert
            Assert.AreEqual(0, setCount);
        }

        [TestMethod]
        public async void ExecuteSMembersShouldReturnFalseOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var result = await adapter.SMembersAsync("test_key_set");

            //Assert
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public async void ExecuteSAddAndSMembersSuccessful()
        {
            //Arrange
            var setCount = await adapter.SAddAsync("test_key_set_multiple", new RedisValue[]{ "test_value", "test_value2"});

            //Act
            var result = await adapter.SMembersAsync("test_key_set_multiple");

            //Assert
            Assert.AreEqual(2, setCount);
            Assert.AreEqual(2, result.Length);
            Assert.IsTrue(result.Contains("test_value"));
            Assert.IsTrue(result.Contains("test_value2"));
        }

        [TestMethod]
        public async void ExecuteSAddShouldReturnZeroOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var setCount = await adapter.SAddAsync("test_key_set_multiple", new RedisValue[] { "test_value", "test_value2" });

            //Assert
            Assert.AreEqual(0, setCount);
        }

        [TestMethod]
        public async void ExecuteSAddAndSRemSuccessful()
        {
            //Arrange
            var setCount = await adapter.SAddAsync("test_key_set", new RedisValue[] { "test_value", "test_value2" });

            //Act
            var remCount = await adapter.SRemAsync("test_key_set", new RedisValue[] { "test_value2" });
            var result = await adapter.SIsMemberAsync("test_key_set", "test_value");
            var result2 = await adapter.SIsMemberAsync("test_key_set", "test_value2");
            var result3 = await adapter.SIsMemberAsync("test_key_set", "test_value3");
            
            //Assert
            Assert.IsTrue(result);
            Assert.IsFalse(result2);
            Assert.IsFalse(result3);
        }

        [TestMethod]
        public async void ExecuteSRemShouldReturnZeroOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var remCount = await adapter.SRemAsync("test_key_set", new RedisValue[] { "test_value2" });

            //Assert
            Assert.AreEqual(0, remCount);
        }

        [TestMethod]
        public async void ExecuteIncrBySuccessful()
        {
            //Arrange
            await adapter.IcrByAsync("test_count", 1);

            //Act
            var result = await adapter.IcrByAsync("test_count", 2);

            //Assert
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public async void ExecuteIncrShouldReturnZeroOnException()
        {
            //Arrange
            var config = new RedisConfig();
            var pool = new ConnectionPoolManager(config);

            var adapter = new RedisAdapter(config, pool);

            //Act
            var result = await adapter.IcrByAsync("test_count", 2);

            //Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public async void ExecuteHashIncrementShouldReturnValue()
        {
            //Act & Assert
            var result = await adapter.HashIncrementAsync("test_count", "hashField", 2);
            Assert.AreEqual(2, result);

            result = await adapter.HashIncrementAsync("test_count", "hashField", 2);
            Assert.AreEqual(4, result);

            result = await adapter.HashIncrementAsync("test_count", "hashField", 3);
            Assert.AreEqual(7, result);

            result = await adapter.HashIncrementAsync("test", "hashField", 1);
            Assert.AreEqual(1, result);
        }
    }
}
