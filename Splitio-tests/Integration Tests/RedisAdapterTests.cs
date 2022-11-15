using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
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
            adapter = new RedisAdapter(new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000
            });
            adapter.Flush();
        }

        [TestMethod]
        public void ExecuteSetAndGetSuccessful()
        {
            //Arrange
            var isSet = adapter.Set("test_key", "test_value");

            //Act
            var result = adapter.Get("test_key");

            //Assert
            Assert.IsTrue(isSet);
            Assert.AreEqual("test_value", result);
        }

        [TestMethod]
        public void ExecuteSetShouldReturnFalseOnException()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig());

            //Act
            var isSet = adapter.Set("test_key", "test_value");

            //Assert
            Assert.IsFalse(isSet);
        }

        [TestMethod]
        public void ExecuteGetShouldReturnEmptyOnException()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig());

            //Act
            var result = adapter.Get("test_key");

            //Assert
            Assert.AreEqual(String.Empty, result);
        }

        [TestMethod]
        public void ExecuteMultipleSetAndMultipleGetSuccessful()
        {
            //Arrange
            var isSet1 = adapter.Set("test_key", "test_value");
            var isSet2 = adapter.Set("test_key2", "test_value2");
            var isSet3 = adapter.Set("test_key3", "test_value3");

            //Act
            var result = adapter.MGet(new RedisKey[]{"test_key", "test_key2", "test_key3"});

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(isSet1 & isSet2 & isSet3);
            Assert.AreEqual(3, result.Count());
            Assert.IsTrue(result.Contains("test_value"));
            Assert.IsTrue(result.Contains("test_value2"));
            Assert.IsTrue(result.Contains("test_value3"));
        }

        [TestMethod]
        public void ExecuteGetShouldReturnEmptyArrayOnException()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig());

            //Act
            var result = adapter.MGet(new RedisKey[] { "test_key", "test_key2", "test_key3" });

            //Assert
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void ExecuteMultipleSetAndGetAllKeysWithFilterSuccessful()
        {
            //Arrange
            var isSet1 = adapter.Set("test.test_key", "test_value");
            var isSet2 = adapter.Set("test.test_key2", "test_value2");
            var isSet3 = adapter.Set("test.test_key3", "test_value3");

            //Act
            var result = adapter.Keys("test.*");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(isSet1 & isSet2 & isSet3);
            Assert.AreEqual(3, result.Count());
            Assert.IsTrue(result.Contains("test.test_key"));
            Assert.IsTrue(result.Contains("test.test_key2"));
            Assert.IsTrue(result.Contains("test.test_key3"));
        }

        [TestMethod]
        public void ExecuteKeysShouldReturnEmptyArrayOnException()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig());

            //Act
            var result = adapter.Keys("test.*");

            //Assert
            Assert.AreEqual(0, result.Count());
        }


        [TestMethod]
        public void ExecuteSetAndDelSuccessful()
        {
            //Arrange
            var isSet1 = adapter.Set("testdel.test_key", "test_value");

            //Act
            var isDel = adapter.Del("testdel.test_key");
            var result = adapter.Get("testdel.test_key");

            //Assert
            Assert.IsTrue(isSet1);
            Assert.IsTrue(isDel);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ExecuteDelShouldReturnFalseOnException()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig());

            //Act
            var isDel = adapter.Del("testdel.test_key");

            //Assert
            Assert.IsFalse(isDel);
        }

        [TestMethod]
        public void ExecuteSetAndFlushSuccessful()
        {
            //Arrange
            var isSet1 = adapter.Set("testflush.test_key", "test_value");

            //Act
            adapter.Flush();
            var result = adapter.Keys("test.*");

            //Assert
            Assert.IsTrue(isSet1);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void ExecuteSAddAndSMemberSuccessful()
        {
            //Arrange
            var setCount = adapter.SAdd("test_key_set", "test_value_1");

            //Act
            var result = adapter.SMembers("test_key_set");

            //Assert
            Assert.AreEqual(true, setCount);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains("test_value_1"));
        }

        [TestMethod]
        public void ExecuteSAddShouldReturnFalseOnException()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig());

            //Act
            var setCount = adapter.SAdd("test_key_set", "test_value_1");

            //Assert
            Assert.IsFalse(setCount);
        }

        [TestMethod]
        public void ExecuteSMembersShouldReturnFalseOnException()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig());

            //Act
            var result = adapter.SMembers("test_key_set");

            //Assert
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void ExecuteSAddAndSMembersSuccessful()
        {
            //Arrange
            var setCount = adapter.SAdd("test_key_set_multiple", new RedisValue[]{ "test_value", "test_value2"});

            //Act
            var result = adapter.SMembers("test_key_set_multiple");

            //Assert
            Assert.AreEqual(2, setCount);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains("test_value"));
            Assert.IsTrue(result.Contains("test_value2"));
        }

        [TestMethod]
        public void ExecuteSAddShouldReturnZeroOnException()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig());

            //Act
            var setCount = adapter.SAdd("test_key_set_multiple", new RedisValue[] { "test_value", "test_value2" });

            //Assert
            Assert.AreEqual(0, setCount);
        }

        [TestMethod]
        public void ExecuteSAddAndSRemSuccessful()
        {
            //Arrange
            var setCount = adapter.SAdd("test_key_set", new RedisValue[] { "test_value", "test_value2" });

            //Act
            var remCount = adapter.SRem("test_key_set", new RedisValue[] { "test_value2" });
            var result = adapter.SIsMember("test_key_set", "test_value");
            var result2 = adapter.SIsMember("test_key_set", "test_value2");
            var result3 = adapter.SIsMember("test_key_set", "test_value3");
            
            //Assert
            Assert.IsTrue(result);
            Assert.IsFalse(result2);
            Assert.IsFalse(result3);
        }

        [TestMethod]
        public void ExecuteSRemShouldReturnZeroOnException()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig());

            //Act
            var remCount = adapter.SRem("test_key_set", new RedisValue[] { "test_value2" });

            //Assert
            Assert.AreEqual(0, remCount);
        }

        [TestMethod]
        public void ExecuteIncrBySuccessful()
        {
            //Arrange
            adapter.IcrBy("test_count", 1);

            //Act
            var result = adapter.IcrBy("test_count", 2);

            //Assert
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void ExecuteIncrShouldReturnZeroOnException()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig());

            //Act
            var result = adapter.IcrBy("test_count", 2);

            //Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetConfigWithoutTls()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000
            });

            //Act
            var result = adapter.GetConfig();

            //Assert
            Assert.AreEqual(1, result.EndPoints.Count);
            Assert.AreEqual(string.Empty, result.Password);
            Assert.AreEqual(1000, result.ConnectTimeout);
            Assert.AreEqual(1000, result.SyncTimeout);
            Assert.IsFalse(result.Ssl);
        }

        [TestMethod]
        public void GetConfigWithTls()
        {
            //Arrange
            var adapter = new RedisAdapter(new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                TlsConfig = new TlsConfig(ssl: true)
            });
            //Act
            var result = adapter.GetConfig();

            //Assert
            Assert.AreEqual(1, result.EndPoints.Count);
            Assert.AreEqual(string.Empty, result.Password);
            Assert.AreEqual(1000, result.ConnectTimeout);
            Assert.AreEqual(1000, result.SyncTimeout);
            Assert.IsTrue(result.Ssl);
            Assert.AreEqual("localhost", result.SslHost);
        }

        [TestMethod]
        public void ExecuteHashIncrementShouldReturnValue()
        {
            //Act & Assert
            var result = adapter.HashIncrement("test_count", "hashField", 2);
            Assert.AreEqual(2, result);

            result = adapter.HashIncrement("test_count", "hashField", 2);
            Assert.AreEqual(4, result);

            result = adapter.HashIncrement("test_count", "hashField", 3);
            Assert.AreEqual(7, result);

            result = adapter.HashIncrement("test", "hashField", 1);
            Assert.AreEqual(1, result);
        }
    }
}
