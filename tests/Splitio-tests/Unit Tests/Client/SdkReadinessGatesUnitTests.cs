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
        private bool SdkReady = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> PublicSdkUpdateHandler;

        [TestMethod]
        public void IsSDKReadyShouldReturnFalseIfSplitsAreNotReady()
        {
            //Arrange
            var gates = new InMemoryReadinessGatesCache(new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig()));

            //Act
            var result = gates.IsReady();

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestFireReadyEvent()
        {
            //Arrange
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig());
            Splitio.Util.Helper.BuildInternalSdkEventStatus(eventsManager);
            var gates = new InMemoryReadinessGatesCache(eventsManager);
            PublicSdkUpdateHandler += sdkReady_callback;
            eventsManager.Register(SdkEvent.SdkReady, sdkReady_callback);

            //Act
            gates.SetReady();

            // Assert.
            Assert.IsTrue(SdkReady);
            Assert.AreEqual(0, eMetadata.GetData().Count);
        }

        private void sdkReady_callback(object sender, EventMetadata metadata)
        {
            SdkReady = true;
            eMetadata = metadata;
        }
    }
}
