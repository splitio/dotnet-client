using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Impressions.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class RedisImpressionsLogUnitTests
    {
        private Mock<IImpressionsCache> _impressionsCache;
        private RedisImpressionLog _redisImpressionLog;

        [TestInitialize]
        public void Initialization()
        {
            _impressionsCache = new Mock<IImpressionsCache>();

            _redisImpressionLog = new RedisImpressionLog(_impressionsCache.Object);
        }

        [TestMethod]
        public void LogSuccessfully()
        {
            //Arrange
            var impressions = new List<KeyImpression>
            {
                new KeyImpression { KeyName = "GetTreatment", Feature = "test", Treatment = "on", Time = 7000, ChangeNumber = 1, Label = "test" }
            };

            //Act
            _redisImpressionLog.Log(impressions);

            //Assert
            _impressionsCache.Verify(mock => mock.Add(It.IsAny<IList<KeyImpression>>()), Times.Once());
        }

        [TestMethod]
        public void LogSuccessfullyUsingBucketingKey()
        {
            //Arrange
            var key = new Key(bucketingKey: "a", matchingKey: "testkey");

            var impressions = new List<KeyImpression>
            {
                new KeyImpression { KeyName = key.matchingKey, Feature = "test", Treatment = "on", Time = 7000, ChangeNumber = 1, Label = "test-label", BucketingKey = key.bucketingKey }
            };

            //Act
            _redisImpressionLog.Log(impressions);

            //Assert
            _impressionsCache
                .Verify(mock => mock.Add(It.Is<IList<KeyImpression>>(v => v.Any(ki => ki.KeyName == key.matchingKey && 
                                                                                      ki.Feature == "test" &&
                                                                                      ki.Treatment == "on" &&
                                                                                      ki.Time == 7000 &&
                                                                                      ki.ChangeNumber == 1 &&
                                                                                      ki.Label == "test-label" &&
                                                                                      ki.BucketingKey == key.bucketingKey))), Times.Once());
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void Start_ReturnsException()
        {
            //Act
            _redisImpressionLog.Start();
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task Stop_ReturnsException()
        {
            //Act
            await _redisImpressionLog.StopAsync();
        }
    }
}
