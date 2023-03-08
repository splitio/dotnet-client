using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Domain;

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
    }
}
