using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventHandler = Splitio.Services.Common.EventHandler;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class SdkEventsTest
    {
        private bool _sdkUpdate = false;
        private bool _sdkReady = false;
        private EventMetadata _eventMetadata;

        [TestMethod]
        public void TriggerAndCatchEventsTest()
        {
            //Arrange
            EventsManagerConfig config = EventsManagerConfig.BuildEventsManagerConfig();
            EventsManager eventsManager = new EventsManager();
            EventDelivery eventDelivery = new EventDelivery(eventsManager);
            eventDelivery.Start();
            EventHandler eventHandler = new EventHandler(config, eventsManager, eventDelivery);
            eventsManager.Register(SdkEvent.SdkUpdate, sdkUpdate_callback);
            eventsManager.Register(SdkEvent.SdkReady, sdkReady_callback);

            Dictionary<string, object> metaData = new Dictionary<string, object>
            {
                { "flags", new List<string> {{ "flag1" }} }
            };
            Dictionary<string, object> metaData2 = new Dictionary<string, object>();

            eventsManager.OnSdkInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(metaData2));
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(metaData2));
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            Assert.IsFalse(_sdkUpdate);
            Assert.IsFalse(_sdkReady);

            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SdkReady, new EventMetadata(metaData2));
            Thread.Sleep(2000);
            Assert.IsTrue(_sdkReady);
            Assert.IsFalse(_sdkUpdate);

            _sdkReady = false;
            _sdkUpdate = false;
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            Thread.Sleep(2000);
            Assert.IsTrue(_sdkUpdate);
            Assert.IsFalse(_sdkReady);

            _sdkUpdate = false;
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(metaData));
            Thread.Sleep(2000);
            Assert.IsTrue(_sdkUpdate);
            Assert.IsFalse(_sdkReady);

            eventDelivery.Stop();
            eventsManager.Destroy();
        }

        private void sdkUpdate_callback(EventMetadata metadata) 
        {
            _sdkUpdate = true;
            _eventMetadata = metadata;
        }

        private void sdkReady_callback(EventMetadata metadata)
        {
            _sdkReady = true;
            _eventMetadata = metadata;
        }
    }
}
