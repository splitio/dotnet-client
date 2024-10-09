using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Domain;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class ConnectionPoolManagerTests
    {
        [TestMethod]
        public void GetConnectionsSuccess()
        {
            // Arrange.
            var config = new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 5,
            };
            var pool = new ConnectionPoolManager(config);


            // Act.
            var conn1 = pool.GetConnection();
            var conn2 = pool.GetConnection();
            var conn3 = pool.GetConnection();
            var conn4 = pool.GetConnection();
            var conn5 = pool.GetConnection();

            // Assert.
            Assert.IsTrue(conn1.IsConnected);
            Assert.IsTrue(conn2.IsConnected);
            Assert.IsTrue(conn3.IsConnected);
            Assert.IsTrue(conn4.IsConnected);
            Assert.IsTrue(conn5.IsConnected);
        }

        [TestMethod]
        public void GetConnectionsShouldReturnNull()
        {
            // Arrange.
            var config = new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 0,
            };
            var pool = new ConnectionPoolManager(config);


            // Act.
            var conn1 = pool.GetConnection();
            var conn2 = pool.GetConnection();

            // Assert.
            Assert.IsNull(conn1);
            Assert.IsNull(conn2);
        }

        [TestMethod]
        public void GetRedisClusterConnectionSuccess()
        {
            // Arrange.
            var config = new RedisConfig
            {
                ClusterNodes = new Splitio.Domain.ClusterNodes(new List<string>() { "localhost:6379" }, "{SPLITIO}"),
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 5,
            };
            var pool = new ConnectionPoolManager(config);

            // Act.
            var conn1 = pool.GetConnection();
            var conn2 = pool.GetConnection();

            // Assert.
            Assert.IsTrue(conn1.IsConnected);
            Assert.IsTrue(conn2.IsConnected);
        }

        [TestMethod]
        public void GetConnectionUsingConnectionString()
        {
            // Arrange.
            var config = new RedisConfig
            {
                ConnectionString = "localhost:6379,DefaultDatabase=0",
                PoolSize = 5
            };
            var pool = new ConnectionPoolManager(config);

            // Act.
            var conn1 = pool.GetConnection();

            // Assert.
            Assert.IsTrue(conn1.IsConnected);
        }

        [TestMethod]
        public void TestConnectionStringIgnoreOtherProperties()
        {
            // Arrange.
            var config = new RedisConfig
            {
                ConnectionString = "localhost:6379,DefaultDatabase=0",
                RedisHost = "invlaidName",
                RedisPort = "0000",
                RedisDatabase = 99999,
                PoolSize = 5
            };
            var pool = new ConnectionPoolManager(config);
            var conn1 = pool.GetConnection();
            Assert.IsTrue(conn1.IsConnected);

            config = new RedisConfig
            {
                ConnectionString = "localhost:6379,DefaultDatabase=0",
                ClusterNodes = new Splitio.Domain.ClusterNodes(new List<string>() { "invalid:0000" }, "{SPLITIO}"),
                PoolSize = 5
            };
            var pool2 = new ConnectionPoolManager(config);
            var conn2 = pool2.GetConnection();
            Assert.IsTrue(conn2.IsConnected);
        }

        [TestMethod]
        public void InvalidConnectionStringReturnNotConnected()
        {
            // Arrange.
            var config = new RedisConfig
            {
                ConnectionString = "invalidhost:0000,invalidOption=0",
                PoolSize = 5
            };
            var pool = new ConnectionPoolManager(config);
            var conn1 = pool.GetConnection();
            Assert.IsNull(conn1);
        }
    }
}
