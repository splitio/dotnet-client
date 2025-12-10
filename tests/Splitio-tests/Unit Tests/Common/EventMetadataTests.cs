using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Splitio.Domain;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class EventMetadataTests
    {

        [TestMethod]
        public void InstanceWithSantizeInput()
        {
            //Arrange
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "updatedFlags", new List<string>() { { "feature1" } } },
                { "sdkTimeout", 10 },
                { "boolValue", true },
                { "strValue", "value" }
            };

            //Act
            var result = new EventMetadata(data);

            //Assert
            Assert.AreEqual(4, result.GetKeys().Count);
            result.GetData().TryGetValue("updatedFlags", out var features);
            List<string> featureList = (List<string>)features;
            result.GetData().TryGetValue("sdkTimeout", out var timeout);
            result.GetData().TryGetValue("boolValue", out var bvalue);
            result.GetData().TryGetValue("strValue", out var svalue);

            Assert.AreEqual(10, timeout);
            Assert.IsTrue((bool)bvalue);
            Assert.AreEqual("value", svalue);
            Assert.IsTrue(featureList.Count == 1);
            Assert.IsTrue(featureList.Contains("feature1"));
            Assert.IsTrue(result.ContainKey("updatedFlags"));
        }

        [TestMethod]
        public void SantizeNullInput()
        {
            //Arrange
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "wrong-null", null },
                { "wrong-other", new List<int>() },
                { "updatedFlags", new List<string>() { { "feature1" } } }
            };

            //Act
            var result = new EventMetadata(data);

            //Assert
            Assert.AreEqual(1, result.GetKeys().Count);
            Assert.IsTrue(result.ContainKey("updatedFlags"));
        }
    }
}
