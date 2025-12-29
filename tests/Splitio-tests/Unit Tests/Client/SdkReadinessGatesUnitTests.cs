using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
using System;

namespace Splitio_Tests.Unit_Tests.Client
{
    [TestClass]
    public class InMemoryReadinessGatesCacheUnitTests
    {
        private bool SdkReadyFlag = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> SdkReady;

        [TestMethod]
        public void IsSDKReadyShouldReturnFalseIfSplitsAreNotReady()
        {
            //Arrange
            var gates = new InMemoryReadinessGatesCache(new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig(), new EventDelivery<SdkEvent, EventMetadata>()));

            //Act
            var result = gates.IsReady();

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestFireReadyEvent()
        {
            //Arrange
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig(), new EventDelivery<SdkEvent, EventMetadata>());
            var gates = new InMemoryReadinessGatesCache(eventsManager);
            SdkReady += sdkReady_callback;
            eventsManager.Register(SdkEvent.SdkReady, TriggerSdkReady);

            //Act
            gates.SetReady();

            // Assert.
            Assert.IsTrue(SdkReadyFlag);
            Assert.AreEqual(0, eMetadata.GetData().Count);
        }

        private void sdkReady_callback(object sender, EventMetadata metadata)
        {
            SdkReadyFlag = true;
            eMetadata = metadata;
        }

        private void TriggerSdkReady(EventMetadata metaData)
        {
            SdkReady?.Invoke(this, metaData);
        }
    }
}
