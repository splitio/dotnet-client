using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Common;
using System.Collections.Generic;
using EventHandler = Splitio.Services.Common.EventHandler;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class EventHandlerTest
    {

        [TestMethod]
        public void TriggerAndCatchEventsTest()
        {
            //Arrange
            EventsManagerConfig config = EventsManagerConfig.BuildEventsManagerConfig();
            EventsManager eventsManager = new EventsManager();
            Mock<IEventDelivery> eventDelivery = new Mock<IEventDelivery>();
            EventHandler eventHandler = new EventHandler(config, eventsManager, eventDelivery.Object);
            eventsManager.Register(SdkEvent.SdkUpdate, sdkUpdate_callback);
            eventsManager.Register(SdkEvent.SdkReady, sdkReady_callback);

            Dictionary<string, object> metaData = new Dictionary<string, object>
            {
                { "flags", new List<string> {{ "flag1" }} }
            };
            EventMetadata eventMetadata = new EventMetadata(metaData);
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, eventMetadata);
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SegmentsUpdated, eventMetadata);
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagsUpdated, eventMetadata);

            eventDelivery.Verify(mock => mock.Deliver(It.IsAny <SdkEvent>(), It.IsAny<EventMetadata>()), Times.Never);

            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SdkReady, new EventMetadata(metaData));
            eventDelivery.Verify(mock => mock.Deliver(SdkEvent.SdkReady, It.IsAny<EventMetadata>()), Times.Once);
            eventDelivery.Verify(mock => mock.Deliver(SdkEvent.SdkUpdate, It.IsAny<EventMetadata>()), Times.Never);
            eventsManager.SetSdkEventTriggered(SdkEvent.SdkReady);

            eventDelivery.Reset();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            eventDelivery.Verify(mock => mock.Deliver(SdkEvent.SdkUpdate, It.IsAny<EventMetadata>()), Times.Once);
            eventDelivery.Verify(mock => mock.Deliver(SdkEvent.SdkReady, It.IsAny<EventMetadata>()), Times.Never);
            eventsManager.SetSdkEventTriggered(SdkEvent.SdkUpdate);

            eventDelivery.Reset();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(metaData));
            eventDelivery.Verify(mock => mock.Deliver(SdkEvent.SdkUpdate, It.IsAny<EventMetadata>()), Times.Once);
            eventDelivery.Verify(mock => mock.Deliver(SdkEvent.SdkReady, It.IsAny<EventMetadata>()), Times.Never);
        }

        private void sdkUpdate_callback(EventMetadata metadata) 
        {
        }

        private void sdkReady_callback(EventMetadata metadata)
        {
        }
    }
}
