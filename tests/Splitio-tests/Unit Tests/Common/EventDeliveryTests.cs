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

            eventDelivery.Deliver(SdkEvent.SdkReady, null, SdkReadyCallback);
            SpinWait.SpinUntil(() => SdkReady, TimeSpan.FromMilliseconds(1000));
            Assert.IsTrue(SdkReady);

            eventDelivery.Deliver(SdkEvent.SdkReady, null, SdkReadyCallbackWithException);
            // should not cause exception here
        }

        private void SdkReadyCallback(EventMetadata metadata)
        {
            SdkReady = true;
            eMetadata = metadata;
        }

        private void SdkReadyCallbackWithException(EventMetadata metadata)
        {
            throw new Exception("something wrong");
        }
    }
}
