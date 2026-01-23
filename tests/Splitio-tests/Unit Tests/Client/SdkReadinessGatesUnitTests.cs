using BitFaster.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using System;
using System.Threading;

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
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig(), new EventDelivery<SdkEvent, EventMetadata>());
            var internalEventsTask = new InternalEventsTask(eventsManager, new SplitQueue<Splitio.Services.EventSource.Workers.SdkEventNotification>());
            var gates = new InMemoryReadinessGatesCache(internalEventsTask);

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
            var internalEventsTask = new InternalEventsTask(eventsManager, new SplitQueue<Splitio.Services.EventSource.Workers.SdkEventNotification>());
            var gates = new InMemoryReadinessGatesCache(internalEventsTask);
            internalEventsTask.Start();
            SdkReady += sdkReady_callback;
            eventsManager.Register(SdkEvent.SdkReady, TriggerSdkReady);

            //Act
            gates.SetReady();
            SpinWait.SpinUntil(() => SdkReadyFlag, TimeSpan.FromMilliseconds(1000));

            // Assert.
            Assert.IsTrue(SdkReadyFlag);
            Assert.AreEqual(null, eMetadata);
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
