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
        private bool SdkTimedOut = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> PublicSdkUpdateHandler;

        [TestMethod]
        public void TestFireTimedOutEvent()
        {
            //Arrange
            Mock<IStatusManager> statusManager = new Mock<IStatusManager>();
            Mock<ITelemetryInitProducer> telemetryProducer = new Mock<ITelemetryInitProducer>();
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig());
            Splitio.Util.Helper.BuildInternalSdkEventStatus(eventsManager);
            var bur = new SelfRefreshingBlockUntilReadyService(statusManager.Object, telemetryProducer.Object, eventsManager);
            PublicSdkUpdateHandler += SdkTimedOut_callback;
            eventsManager.Register(SdkEvent.SdkReadyTimeout, SdkTimedOut_callback);
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
            Assert.IsTrue(SdkTimedOut);
            Assert.AreEqual(0, eMetadata.GetData().Count);
        }

        private void SdkTimedOut_callback(object sender, EventMetadata metadata)
        {
            SdkTimedOut = true;
            eMetadata = metadata;
        }
    }
}
