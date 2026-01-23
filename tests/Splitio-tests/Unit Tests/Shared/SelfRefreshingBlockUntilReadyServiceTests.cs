using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Storages;
using System;

namespace Splitio_Tests.Unit_Tests.Shared
{
    [TestClass]
    public class SelfRefreshingBlockUntilReadyServiceTests
    {
        private bool SdkTimedOutFlag = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> SdkTimedOut;

        [TestMethod]
        public void TestFireTimedOutEvent()
        {
            //Arrange
            Mock<IStatusManager> statusManager = new Mock<IStatusManager>();
            Mock<ITelemetryInitProducer> telemetryProducer = new Mock<ITelemetryInitProducer>();
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig(), new EventDelivery<SdkEvent, EventMetadata>());
            var bur = new SelfRefreshingBlockUntilReadyService(statusManager.Object, telemetryProducer.Object, eventsManager);
            SdkTimedOut += sdkTimeout_callback;
            eventsManager.Register(SdkEvent.SdkReadyTimeout, TriggerSdkTimedOut);
            statusManager
                .Setup(mock => mock.WaitUntilReady(1))
                .Returns(false);

            //Act
            try
            {
                bur.BlockUntilReady(1);
            }
            catch { }

            // Assert.
            Assert.IsTrue(SdkTimedOutFlag);
            Assert.AreEqual(null, eMetadata);
        }

        private void sdkTimeout_callback(object sender, EventMetadata metadata)
        {
            SdkTimedOutFlag = true;
            eMetadata = metadata;
        }

        private void TriggerSdkTimedOut(EventMetadata metaData)
        {
            SdkTimedOut?.Invoke(this, metaData);
        }
    }
}
