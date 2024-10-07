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
        public void BuildRedisSplitClientWithoutHashTagShouldSetValue()
        {
            //Arrange           
            var configurationOptions = new ConfigurationOptions
            {
                Mode = Mode.Consumer,
                CacheAdapterConfig = new CacheAdapterConfigurationOptions { RedisClusterNodes = new Splitio.Domain.ClusterNodes(new List<string>() { "localhost:6379" }, null) }
            };
            RedisConfigurationValidator.Validate(configurationOptions.CacheAdapterConfig);
            Assert.AreEqual("{SPLITIO}", configurationOptions.CacheAdapterConfig.RedisClusterNodes.KeyHashTag);
        }

        [TestMethod]
        public void UseConnectionStringIgnoresOtherProperties()
        {
            //Arrange           
            var configurationOptions = new ConfigurationOptions
            {
                Mode = Mode.Consumer,
                CacheAdapterConfig = new CacheAdapterConfigurationOptions { ConnectionString = "localhost:6379"}
            };
            RedisConfigurationValidator.Validate(configurationOptions.CacheAdapterConfig);
        }
    }
}
