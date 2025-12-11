using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using System;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class EventDeliveryTests
    {
        private bool SdkReady = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> PublicSdkReadyHandler;

        [TestMethod]
        public void TestFiringEvents()
        {
            //Act
            EventDelivery eventDelivery = new EventDelivery();

            PublicSdkReadyHandler += sdkReady_callback;

            Dictionary<string, object> metaData = new Dictionary<string, object>
            {
                { "flags", new List<string> {{ "flag1" }} }
            };

            eventDelivery.Deliver(SdkEvent.SdkReady, new EventMetadata(metaData), PublicSdkReadyHandler);

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

        private void sdkReady_callback(object sender, EventMetadata metadata)
        {
            SdkReady = true;
            eMetadata = metadata;
        }
    }
}
