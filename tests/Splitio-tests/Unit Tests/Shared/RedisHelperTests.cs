using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Redis.Services.Domain;
using Splitio.Redis.Services.Shared;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class RedisHelperTests
    {
        private X509Certificate2 CertificateSelection(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return null;
        }

        private bool CertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        [TestMethod]
        public void TestCorrectSingleHostConfigSuccess()
        {
            // Arrange.
            var tlsConfig = new TlsConfig(ssl: true)
            {
                CertificateValidationFunc = CertificateValidation,
                CertificateSelectionFunc = CertificateSelection
            };
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
                TlsConfig = tlsConfig
            };
            bool isClusterMode = false;
            var options = Helper.ParseFromRedisConfig(config, ref isClusterMode);

            // Assert.
            Assert.AreEqual(false, isClusterMode);
            Assert.AreEqual(1, options.EndPoints.Count);
            Assert.AreEqual("Unspecified/localhost:6379", options.EndPoints[0].ToString());
            Assert.AreEqual(1000, options.ConnectTimeout);
            Assert.AreEqual(1000, options.SyncTimeout);
            Assert.AreEqual(5, options.ConnectRetry); 
            Assert.AreEqual("", options.Password);
            Assert.AreEqual(true, options.AllowAdmin);
            Assert.AreEqual(1, options.KeepAlive);
            Assert.AreEqual(true, options.Ssl);
        }

        [TestMethod]
        public void TestCorrectClusterConfigSuccess()
        {
            // Arrange.
            var config = new RedisConfig
            {
                ClusterNodes = new Splitio.Domain.ClusterNodes(
                    new List<string>() { "localhost:6379", "localhost:6380" }, "{split}"
                ),
                RedisPassword = "mypass",
                RedisDatabase = 1,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                PoolSize = 5,
            };
            bool isClusterMode = false;
            var options = Helper.ParseFromRedisConfig(config, ref isClusterMode);

            // Assert.
            Assert.AreEqual(true, isClusterMode);
            Assert.AreEqual(2, options.EndPoints.Count);
            Assert.AreEqual("Unspecified/localhost:6379", options.EndPoints[0].ToString());
            Assert.AreEqual("Unspecified/localhost:6380", options.EndPoints[1].ToString());
            Assert.AreEqual(1000, options.ConnectTimeout);
            Assert.AreEqual(1000, options.SyncTimeout);
            Assert.AreEqual("mypass", options.Password);
            Assert.AreEqual(true, options.AllowAdmin);
            Assert.AreEqual(1, options.KeepAlive);
        }

        [TestMethod]
        public void TestCorrectConnectionStringClusterSuccess()
        {
            // Arrange.
            var config = new RedisConfig
            {
                ConnectionString = "localhost:6379,localhost:6380,ConnectTimeout=1300,SyncTimeout=1500"
            };
            bool isClusterMode = false;
            var options = Helper.ParseFromConnectionString(config, ref isClusterMode);

            // Assert.
            Assert.AreEqual(true, isClusterMode);
            Assert.AreEqual(2, options.EndPoints.Count);
            Assert.AreEqual("Unspecified/localhost:6379", options.EndPoints[0].ToString());
            Assert.AreEqual("Unspecified/localhost:6380", options.EndPoints[1].ToString());
            Assert.AreEqual(1300, options.ConnectTimeout);
            Assert.AreEqual(1500, options.SyncTimeout);
            Assert.AreEqual(true, options.AllowAdmin);
            Assert.AreEqual(1, options.KeepAlive);
        }

        [TestMethod]
        public void TestCorrectConnectionStringSingleHostSuccess()
        {
            // Arrange.
            var config = new RedisConfig
            {
                ConnectionString = "localhost:6379,ConnectTimeout=1300,SyncTimeout=1500"
            };
            bool isClusterMode = false;
            var options = Helper.ParseFromConnectionString(config, ref isClusterMode);

            // Assert.
            Assert.AreEqual(false, isClusterMode);
            Assert.AreEqual(1, options.EndPoints.Count);
            Assert.AreEqual("Unspecified/localhost:6379", options.EndPoints[0].ToString());
            Assert.AreEqual(1300, options.ConnectTimeout);
            Assert.AreEqual(1500, options.SyncTimeout);
            Assert.AreEqual(true, options.AllowAdmin);
            Assert.AreEqual(1, options.KeepAlive);
        }

        [TestMethod]
        public void TestInCorrectTimeouts()
        {
            // Arrange.
            var config = new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = -10,
                RedisConnectRetry = -5,
                RedisSyncTimeout = 0,
                PoolSize = 5,
            };
            bool isClusterMode = false;
            var options = Helper.ParseFromRedisConfig(config, ref isClusterMode);

            // Assert.
            Assert.AreNotEqual(-10, options.ConnectTimeout);
            Assert.AreNotEqual(0, options.SyncTimeout);
            Assert.AreNotEqual(-5, options.ConnectRetry);
        }

        [TestMethod]
        public void TestInCorrectConnectionStringReturnEmptyOptions()
        {
            // Arrange.
            var config = new RedisConfig
            {
                ConnectionString = "local,incorrect=nothing"
            };
            bool isClusterMode = false;
            var options = Helper.ParseFromConnectionString(config, ref isClusterMode);

            // Assert.
            Assert.AreEqual(false, isClusterMode);
            Assert.AreEqual(0, options.EndPoints.Count);
        }
    }
}
