using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using System.Collections.Generic;
using System.Threading;
using EventHandler = Splitio.Services.Common.EventHandler;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class EventHandlerTest
    {
        private bool SdkReady = false;
        private bool SdkTimedOut = false;
        private bool SdkUpdate = false;
        private EventMetadata eMetadata = null;

        [TestMethod]
        public void TriggerAndCatchEventsTest()
        {
            //Arrange
            EventsManagerConfig config = EventsManagerConfig.BuildEventsManagerConfig();
            EventsManager eventsManager = new EventsManager();
           
            EventHandler eventHandler = new EventHandler(config, eventsManager);
            eventHandler.SubscribeInternalEvents();

            eventsManager.PublicSdkUpdateHandler += sdkUpdate_callback;
            eventsManager.PublicSdkReadyHandler += sdkReady_callback;
            eventsManager.PublicSdkTimedOutHandler += sdkTimedOut_callback;

            Dictionary<string, object> metaData = new Dictionary<string, object>
            {
                { "flags", new List<string> {{ "flag1" }} }
            };
            EventMetadata eventMetadata = new EventMetadata(metaData);
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, eventMetadata);
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SegmentsUpdated, eventMetadata);
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagsUpdated, eventMetadata);
            Assert.IsFalse(SdkUpdate);
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkTimedOut);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SdkReady, new EventMetadata(metaData));
            Thread.Sleep(500);
            Assert.IsTrue(SdkReady);
            Assert.IsFalse(SdkUpdate);
            Assert.IsFalse(SdkTimedOut);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            Thread.Sleep(500);
            Assert.IsTrue(SdkUpdate);
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkTimedOut);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(metaData));
            Thread.Sleep(500);
            Assert.IsTrue(SdkUpdate);
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkTimedOut);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(metaData));
            Thread.Sleep(500);
            Assert.IsTrue(SdkUpdate);
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkTimedOut);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(metaData));
            Thread.Sleep(500);
            Assert.IsTrue(SdkUpdate);
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkTimedOut);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData));
            Thread.Sleep(500);
            Assert.IsFalse(SdkUpdate);
            Assert.IsFalse(SdkReady);
            Assert.IsTrue(SdkTimedOut);
            VerifyMetadata(eMetadata);
        }

        [TestMethod]
        public void SubscribeInternalEventsTest()
        {
            //Arrange
            EventsManagerConfig config = EventsManagerConfig.BuildEventsManagerConfig();
            EventsManager eventsManager = new EventsManager();

            EventHandler eventHandler = new EventHandler(config, eventsManager);
            eventsManager.PublicSdkTimedOutHandler += sdkTimedOut_callback;
            EventMetadata eventMetadata = new EventMetadata(new Dictionary<string, object>());

            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SdkTimedOut, eventMetadata);
            Assert.IsFalse(SdkTimedOut);

            SdkTimedOut = false;
            eventHandler.SubscribeInternalEvents();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SdkTimedOut, eventMetadata);
            Assert.IsTrue(SdkTimedOut);

            SdkTimedOut = false;
            eventHandler.ClearInternalEventsSubscription();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SdkTimedOut, eventMetadata);
            Assert.IsFalse(SdkTimedOut);
        }

        void ResetAllVariables()
        {
            SdkReady = false;
            SdkTimedOut = false;
            eMetadata = null;
            SdkUpdate = false;
        }

        void VerifyMetadata(EventMetadata eMetdata)
        {
            Assert.IsTrue(eMetadata.ContainKey("flags"));
            List<string> flags = (List<string>)eMetadata.GetData()["flags"];
            Assert.IsTrue(flags.Count == 1);
            Assert.IsTrue(flags.Contains("flag1"));
        }

        private void sdkUpdate_callback(object sender, EventMetadata metadata)
        {
            SdkUpdate = true;
            eMetadata = metadata;
        }

        private void sdkReady_callback(object sender, EventMetadata metadata)
        {
            SdkReady = true;
            eMetadata = metadata;
        }

        private void sdkTimedOut_callback(object sender, EventMetadata metadata)
        {
            SdkTimedOut = true;
            eMetadata = metadata;
        }
    }
}
