using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using System;
using System.Threading;

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

            eventDelivery.Deliver(SdkEvent.SdkReady, null, sdkReady_callback);
            SpinWait.SpinUntil(() => SdkReady, TimeSpan.FromMilliseconds(1000));

            Assert.IsTrue(SdkReady);
        }

        private void sdkReady_callback(EventMetadata metadata)
        {
            SdkReady = true;
            eMetadata = metadata;
        }
    }
}
