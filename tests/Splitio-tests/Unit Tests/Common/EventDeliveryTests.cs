using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class EventDeliveryTests
    {
        private bool SdkReady = false;
        private EventMetadata eMetadata = null;

        [TestMethod]
        public void TestFiringEvents()
        {
            //Act
            EventDelivery<SdkEvent, EventMetadata> eventDelivery = new EventDelivery<SdkEvent, EventMetadata>();

            Dictionary<string, object> metaData = new Dictionary<string, object>
            {
                { "flags", new List<string> {{ "flag1" }} }
            };

            eventDelivery.Deliver(SdkEvent.SdkReady, new EventMetadata(metaData), sdkReady_callback);

            Assert.IsTrue(SdkReady);
            VerifyMetadata(eMetadata);
        }

        void VerifyMetadata(EventMetadata eMetdata)
        {
            Assert.IsTrue(eMetadata.ContainKey("flags"));
            List<string> flags = (List<string>)eMetadata.GetData()["flags"];
            Assert.IsTrue(flags.Count == 1);
            Assert.IsTrue(flags.Contains("flag1"));
        }

        private void sdkReady_callback(EventMetadata metadata)
        {
            SdkReady = true;
            eMetadata = metadata;
        }
    }
}
