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
        [ExpectedException(typeof(ArgumentNullException), "Redis Host, Port and Password should be set to initialize Split SDK in Redis Mode.")]
        public void BuildRedisClientWithoutAllRequiredConfigsShouldReturnException()
        {
            //Arrange           
            var configurationOptions = new ConfigurationOptions
            {
                Mode = Mode.Consumer,
                CacheAdapterConfig = new CacheAdapterConfigurationOptions { Host = "local" }
            };
            RedisConfigurationValidator.ValidateRedisOptions(configurationOptions);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "Redis Host, Port and Password should be set to initialize Split SDK in Redis Mode.")]
        public void BuildRedisClusterClientWithoutHostsShouldReturnException()
        {
            //Arrange           
            var configurationOptions = new ConfigurationOptions
            {
                Mode = Mode.Consumer,
                CacheAdapterConfig = new CacheAdapterConfigurationOptions { RedisClusterNodes = new ClusterNodes(new List<string>() { }, "null") }
            };
            RedisConfigurationValidator.ValidateRedisOptions(configurationOptions);
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
            RedisConfigurationValidator.ValidateRedisOptions(configurationOptions);
            Assert.AreEqual("{SPLITIO}", configurationOptions.CacheAdapterConfig.RedisClusterNodes.KeyHashTag);
        }

    }
}
