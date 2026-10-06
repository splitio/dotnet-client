using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Splitio.Commons.Dto;
using Splitio.Commons.Domain;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class EventMetadataTests
    {

        [TestMethod]
        public void InstanceWithInput()
        {
            //Act
            var result = new EventMetadata(SdkEventType.FlagsUpdate, new List<string>() { { "feature1" } });

            //Assert
            Assert.AreEqual(1, result.GetNames().Count);
            Assert.AreEqual(SdkEventType.FlagsUpdate, result.GetEventType());
            Assert.IsTrue(result.GetNames().Contains("feature1"));
        }
    }
}
