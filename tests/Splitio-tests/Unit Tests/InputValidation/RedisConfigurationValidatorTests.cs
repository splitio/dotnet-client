using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Client.Classes;
using Splitio.Domain;
using System;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.InputValidation
{
    [TestClass]
    public class RedisConfigurationValidatorTests
    {
        [TestMethod]
        [ExpectedException(typeof(Exception), "Redis Host, Port and Password should be set to initialize Split SDK in Redis Mode.")]
        public void BuildRedisClientWithoutAllRequiredConfigsShouldReturnException()
        {
            //Arrange           
            var configurationOptions = new ConfigurationOptions
            {
                Mode = Mode.Consumer,
                CacheAdapterConfig = new CacheAdapterConfigurationOptions { Host = "local" }
            };
            RedisConfigurationValidator.Validate(configurationOptions.CacheAdapterConfig);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "Redis Host, Port and Password should be set to initialize Split SDK in Redis Mode.")]
        public void BuildRedisClusterClientWithoutHostsShouldReturnException()
        {
            //Arrange           
            var configurationOptions = new ConfigurationOptions
            {
                Mode = Mode.Consumer,
                CacheAdapterConfig = new CacheAdapterConfigurationOptions { RedisClusterNodes = new ClusterNodes(new List<string>() { }, "null") }
            };
            RedisConfigurationValidator.Validate(configurationOptions.CacheAdapterConfig);
        }

        [TestMethod]
        public void UseConnectionStringIgnoresOtherProperties()
        {
            //Arrange           
            var configurationOptions = new ConfigurationOptions
            {
                Mode = Mode.Consumer,
                CacheAdapterConfig = new CacheAdapterConfigurationOptions { RedisConnectionString = "localhost:6379"}
            };
            RedisConfigurationValidator.Validate(configurationOptions.CacheAdapterConfig);
        }

        [TestMethod]
        public void VerifyAndSetHashTagValue()
        {
            CacheAdapterConfigurationOptions CacheAdapterConfig = new CacheAdapterConfigurationOptions
            {
                RedisClusterNodes = new ClusterNodes(new List<string>() { "localhost:6379" },
                    "{}")
            };
            RedisConfigurationValidator.Validate(CacheAdapterConfig);
            Assert.AreEqual("{SPLITIO}", CacheAdapterConfig.RedisClusterNodes.KeyHashTag);

            CacheAdapterConfig = new CacheAdapterConfigurationOptions
            {
                RedisClusterNodes = new ClusterNodes(new List<string>() { "localhost:6379" },
                "{MyHashTag}")
            };
            RedisConfigurationValidator.Validate(CacheAdapterConfig);
            Assert.AreEqual("{MyHashTag}", CacheAdapterConfig.RedisClusterNodes.KeyHashTag);


            CacheAdapterConfig = new CacheAdapterConfigurationOptions
            {
                RedisClusterNodes = new ClusterNodes(new List<string>() { "localhost:6379" },
                    "{without end bracket")
            };
            RedisConfigurationValidator.Validate(CacheAdapterConfig);
            Assert.AreEqual("{SPLITIO}", CacheAdapterConfig.RedisClusterNodes.KeyHashTag);

            CacheAdapterConfig = new CacheAdapterConfigurationOptions
            {
                RedisClusterNodes = new ClusterNodes(new List<string>() { "localhost:6379" },
                    "without first bracket}")
            };
            RedisConfigurationValidator.Validate(CacheAdapterConfig);
            Assert.AreEqual("{SPLITIO}", CacheAdapterConfig.RedisClusterNodes.KeyHashTag);

            CacheAdapterConfig = new CacheAdapterConfigurationOptions
            {
                RedisClusterNodes = new ClusterNodes(new List<string>() { "localhost:6379" },
            "without any bracket")
            };
            RedisConfigurationValidator.Validate(CacheAdapterConfig);
            Assert.AreEqual("{SPLITIO}", CacheAdapterConfig.RedisClusterNodes.KeyHashTag);

            CacheAdapterConfig = new CacheAdapterConfigurationOptions
            {
                RedisClusterNodes = new ClusterNodes(new List<string>() { "localhost:6379" },
            "")
            };
            RedisConfigurationValidator.Validate(CacheAdapterConfig);
            Assert.AreEqual("{SPLITIO}", CacheAdapterConfig.RedisClusterNodes.KeyHashTag);

            CacheAdapterConfig = new CacheAdapterConfigurationOptions
            {
                RedisClusterNodes = new ClusterNodes(new List<string>() { "localhost:6379" },
            null)
            };
            RedisConfigurationValidator.Validate(CacheAdapterConfig);
            Assert.AreEqual("{SPLITIO}", CacheAdapterConfig.RedisClusterNodes.KeyHashTag);
        }
    }
}
