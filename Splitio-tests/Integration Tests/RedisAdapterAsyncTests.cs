using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Domain;
using Splitio_Tests.Resources;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class RedisAdapterAsyncTests
    {
        private readonly string _redisPrefix = "async-tests";
        private RedisAdapterForTests _adapter;

        [TestInitialize]
        public async Task Initialization()
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
                RedisUserPrefix = _redisPrefix
            };

            var pool = new ConnectionPoolManager(config);
            _adapter = new RedisAdapterForTests(config, pool);

            await CleanKeys();
        }

        [TestMethod]
        public async Task ExecuteSetAsyncAndGetAsyncSuccessful()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key";
            var value = "test_value";
            var isSet = await _adapter.SetAsync(key, value);

            // Act
            var result = await _adapter.GetAsync(key);

            // Assert
            Assert.IsTrue(isSet);
            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public async Task ExecuteSetAsyncAndMGetAsyncSuccessful()
        {
            // Arrange
            var keys = new RedisKey[] { $"{_redisPrefix}test_key1", $"{_redisPrefix}test_key2", $"{_redisPrefix}test_key3" };
            var value = "test_value";

            var count = 1;
            foreach (var key in keys)
            {
                var isSet = await _adapter.SetAsync(key, value + count);
                Assert.IsTrue(isSet);
                count++;
            }

            // Act
            var result = await _adapter.MGetAsync(keys);

            // Assert
            var expected = new RedisValue[] { "test_value1", "test_value2", "test_value3" };
            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public async Task ExecuteSAddAsyncAndSmembersAsyncSuccessful()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key1";
            var values = new RedisValue[] { "value1", "value2" };

            // Act
            foreach (var value in values)
            {
                var success = await _adapter.SAddAsync(key, value);
                Assert.IsTrue(success);
            }

            // Assert
            var result = await _adapter.SMembersAsync(key);
            Assert.AreEqual(2, result.Length);
            Assert.IsTrue(result.Any(r => r == "value1"));
            Assert.IsTrue(result.Any(r => r == "value2"));
        }

        [TestMethod]
        public async Task ExecuteMSAddAsyncAndSmembersAsyncSuccessful()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key1";
            var values = new RedisValue[] { "value1", "value2" };

            // Act
            var count = await _adapter.SAddAsync(key, values);
            Assert.AreEqual(2, count);

            // Assert
            var result = await _adapter.SMembersAsync(key);
            Assert.AreEqual(2, result.Length);
            Assert.IsTrue(result.Any(r=> r == "value1"));
            Assert.IsTrue(result.Any(r => r == "value2"));
        }

        [TestMethod]
        public async Task ExecuteListRightPushAsyncAndListRangeAsyncSuccessful()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key1";
            var value = "value1";

            // Act
            var count = await _adapter.ListRightPushAsync(key, value);
            Assert.AreEqual(1, count);
            count = await _adapter.ListRightPushAsync(key, value);
            Assert.AreEqual(2, count);

            // Assert
            var result = await _adapter.ListRangeAsync(key, 0, -1);
            var expected = new RedisValue[] { value, value };
            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public async Task ExecuteMListRightPushAsyncAndListRangeAsyncSuccessful()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key1";
            var values = new RedisValue[] { "value1", "value2", "value3" };

            // Act
            var count = await _adapter.ListRightPushAsync(key, values);
            Assert.AreEqual(3, count);

            // Assert
            var result = await _adapter.ListRangeAsync(key, 0, -1);
            CollectionAssert.AreEqual(values, result);
        }

        [TestMethod]
        public async Task ExecuteSisMemberAsyncSuccessful()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key1";
            var values = new RedisValue[] { "value1", "value2" };

            await _adapter.SAddAsync(key, values);

            // Act & Assert
            var exists = await _adapter.SIsMemberAsync(key, "value1");
            Assert.IsTrue(exists);

            exists = await _adapter.SIsMemberAsync(key, "value3");
            Assert.IsFalse(exists);

            exists = await _adapter.SIsMemberAsync(key, "value2");
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public async Task ExecuteIcrByAsyncSuccessful()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key1";
            var value = 100;

            // Act && Assert
            var result = await _adapter.IcrByAsync(key, value);
            Assert.AreEqual(value, result);

            result = await _adapter.IcrByAsync(key, value);
            Assert.AreEqual(200, result);
        }

        [TestMethod]
        public async Task ExecuteKeyExpireAsyncSuccessful()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key1";

            await _adapter.SetAsync(key, "value1");

            // Act && Assert

            var success = await _adapter.KeyExpireAsync(key, new TimeSpan(0, 0, 3600));
            Assert.IsTrue(success);

            var result = await _adapter.KeyTimeToLiveAsync(key);
            Assert.IsTrue(result.Value.TotalMinutes >= 55);
        }

        [TestMethod]
        public async Task ExecuteHashIncrementAsyncSuccessful()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key1";
            var field = "field1";

            // Act & Assert
            var count = await _adapter.HashIncrementAsync(key, field, 100);
            Assert.AreEqual(100, count);

            count = await _adapter.HashIncrementAsync(key, field, 50);
            Assert.AreEqual(150, count);

            var hashEntries = await _adapter.HashGetAllAsync(key);
            var expected = new HashEntry(field, 150);
            Assert.AreEqual(expected.Name, hashEntries[0].Name);
            Assert.AreEqual(expected.Value, hashEntries[0].Value);
        }

        [TestMethod]
        public async Task ExecuteHashSetAsyncSuccessful()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key1";
            var field = "field1";

            // Act & Assert
            var added = await _adapter.HashSetAsync(key, field, 100);
            Assert.IsTrue(added);

            var hashEntries = await _adapter.HashGetAllAsync(key);
            var expected = new HashEntry(field, 100);
            Assert.AreEqual(expected.Name, hashEntries[0].Name);
            Assert.AreEqual(expected.Value, hashEntries[0].Value);
        }

        [TestMethod]
        public async Task ExecuteHashIncrementBatchAsyncSuccessful()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key1";
            var values = new Dictionary<string, int> { { "field1", 100 }, { "field2", 60 } };

            // Act & Assert
            var count = await _adapter.HashIncrementBatchAsync(key, values);
            Assert.AreEqual(162, count);
        }

        [TestMethod]
        public async Task KeysAsync()
        {
            // Arrange
            var key = $"{_redisPrefix}test_key";
            var value = "test_value";
            await _adapter.SetAsync(key + "-0", value);
            await _adapter.SetAsync(key + "-1", value);
            await _adapter.SetAsync(key + "-2", value);

            // Act
            var result = await _adapter.KeysAsync($"{_redisPrefix}*");

            // Assert
            Assert.AreEqual(3, result.Length);
            Assert.IsTrue(result.Contains($"{_redisPrefix}test_key-0"));
            Assert.IsTrue(result.Contains($"{_redisPrefix}test_key-1"));
            Assert.IsTrue(result.Contains($"{_redisPrefix}test_key-2"));
        }

        [TestCleanup]
        public async Task CleanKeys()
        {
            var keys = await _adapter.KeysAsync($"{_redisPrefix}*");

            await _adapter.DelAsync(keys);
        }
    }
}
