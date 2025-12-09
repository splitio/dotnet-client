using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using System.Collections.Generic;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class EventsManagerTests
    {
        private bool RuleBasedSegmentsUpdated = false;
        private bool FlagsUpdated = false;
        private bool FlagKilledNotification = false;
        private bool SegmentsUpdated = false;
        private bool SdkReady = false;
        private bool SdkTimedOut = false;
        private EventMetadata eMetadata = null;

        [TestMethod]
        public void TestFiringInternalEvents()
        {
            //Act
            EventsManager eventsManager = new EventsManager();
            eventsManager.RuleBasedSegmentsUpdatedHandler += EventManager_RuleBasedSegmentsUpdatedHandler;
            eventsManager.FlagKilledNotificationHandler += EventManager_FlagKilledNotificationHandler;
            eventsManager.FlagsUpdatedHandler += EventManager_FlagsUpdatedHandler;
            eventsManager.SegmentsUpdatedHandler += EventManager_SegmentsUpdatedHandler;
            eventsManager.SdkReadyHandler += EventManager_SdkReadyHandler;
            eventsManager.SdkTimedOutHandler += EventManager_SdkTimedOutHandler;

            Dictionary<string, object> metaData = new Dictionary<string, object>
            {
                { "flags", new List<string> {{ "flag1" }} }
            };

            eventsManager.OnSdkInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(metaData));
            Thread.Sleep(1000);
            Assert.IsTrue(RuleBasedSegmentsUpdated);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(metaData));
            Thread.Sleep(1000);
            Assert.IsTrue(FlagKilledNotification);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SdkReady, new EventMetadata(metaData));
            Thread.Sleep(1000);
            Assert.IsTrue(SdkReady);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData));
            Thread.Sleep(1000);
            Assert.IsTrue(SdkTimedOut);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            Thread.Sleep(1000);
            Assert.IsTrue(FlagsUpdated);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(metaData));
            Thread.Sleep(1000);
            Assert.IsTrue(SegmentsUpdated);
            VerifyMetadata(eMetadata);
        }

        [TestMethod]
        public void TestRegisterEvents()
        {
            EventsManager eventsManager = new EventsManager();
            eventsManager.Register(SdkEvent.SdkUpdate, sdkUpdate_callback);
            eventsManager.Register(SdkEvent.SdkReady, sdkReady_callback);

            Assert.IsFalse(eventsManager.EventAlreadyTriggered(SdkEvent.SdkReady));
            Assert.IsFalse(eventsManager.EventAlreadyTriggered(SdkEvent.SdkUpdate));
            Assert.IsTrue(eventsManager.IsEventRegistered(SdkEvent.SdkUpdate));
            Assert.IsTrue(eventsManager.IsEventRegistered(SdkEvent.SdkReady));
            Assert.IsFalse(eventsManager.IsEventRegistered(SdkEvent.SdkReadyTimeout));
            Assert.IsTrue(eventsManager.GetCallbackAction(SdkEvent.SdkUpdate) == sdkUpdate_callback);
            Assert.IsTrue(eventsManager.GetCallbackAction(SdkEvent.SdkReady) == sdkReady_callback);
            Assert.IsTrue(eventsManager.GetCallbackAction(SdkEvent.SdkReadyTimeout) == null);

            eventsManager.SetSdkEventTriggered(SdkEvent.SdkReady);
            Assert.IsTrue(eventsManager.EventAlreadyTriggered(SdkEvent.SdkReady));
            Assert.IsFalse(eventsManager.EventAlreadyTriggered(SdkEvent.SdkUpdate));

            eventsManager.Unregister(SdkEvent.SdkUpdate);
            Assert.IsFalse(eventsManager.IsEventRegistered(SdkEvent.SdkUpdate));
            Assert.IsTrue(eventsManager.GetCallbackAction(SdkEvent.SdkUpdate) == null);

            eventsManager.Destroy();
            Assert.IsFalse(eventsManager.IsEventRegistered(SdkEvent.SdkReady));
        }

        [TestMethod]
        public void TestEventsStatus()
        {
            EventsManager eventsManager = new EventsManager();
            Assert.IsFalse(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.SdkReady));
            Assert.IsFalse(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.FlagKilledNotification));
            Assert.IsFalse(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.FlagsUpdated));
            Assert.IsFalse(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.RuleBasedSegmentsUpdated));
            Assert.IsFalse(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.SdkTimedOut));
            Assert.IsFalse(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.SegmentsUpdated));

            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.SdkReady, true);
            Assert.IsTrue(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.SdkReady));

            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.FlagKilledNotification, true);
            Assert.IsTrue(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.FlagKilledNotification));

            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.RuleBasedSegmentsUpdated, true);
            Assert.IsTrue(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.RuleBasedSegmentsUpdated));

            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.SegmentsUpdated, true);
            Assert.IsTrue(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.SegmentsUpdated));

            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.FlagsUpdated, true);
            Assert.IsTrue(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.FlagsUpdated));

            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.SdkTimedOut, true);
            Assert.IsTrue(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.SdkTimedOut));

            eventsManager.UpdateSdkInternalEventStatus(SdkInternalEvent.SdkReady, false);
            Assert.IsFalse(eventsManager.GetSdkInternalEventStatus(SdkInternalEvent.SdkReady));
        }

        void ResetAllVariables()
        {
            RuleBasedSegmentsUpdated = false;
            FlagsUpdated = false;
            FlagKilledNotification = false;
            SegmentsUpdated = false;
            SdkReady = false;
            SdkTimedOut = false;
            eMetadata = null;
        }
        void VerifyMetadata(EventMetadata eMetdata)
        {
            Assert.IsTrue(eMetadata.ContainKey("flags"));
            List<string> flags = (List<string>)eMetadata.GetData()["flags"];
            Assert.IsTrue(flags.Count == 1);
            Assert.IsTrue(flags.Contains("flag1"));
        }

        void EventManager_RuleBasedSegmentsUpdatedHandler(object sender, EventMetadata eventMetadata)
        {
            RuleBasedSegmentsUpdated = true;
            eMetadata = eventMetadata;
        }

        void EventManager_FlagsUpdatedHandler(object sender, EventMetadata eventMetadata)
        {
            FlagsUpdated = true;
            eMetadata = eventMetadata;
        }

        void EventManager_FlagKilledNotificationHandler(object sender, EventMetadata eventMetadata)
        {
            FlagKilledNotification = true;
            eMetadata = eventMetadata;
        }

        void EventManager_SegmentsUpdatedHandler(object sender, EventMetadata eventMetadata)
        {
            SegmentsUpdated = true;
            eMetadata = eventMetadata;
        }

        void EventManager_SdkReadyHandler(object sender, EventMetadata eventMetadata)
        {
            SdkReady = true;
            eMetadata = eventMetadata;
        }

        void EventManager_SdkTimedOutHandler(object sender, EventMetadata eventMetadata)
        {
            SdkTimedOut = true;
            eMetadata = eventMetadata;
        }

        private void sdkUpdate_callback(EventMetadata metadata)
        {
            SdkReady = true;
        }

        private void sdkReady_callback(EventMetadata metadata)
        {
            SdkReady = true;
        }
    }
}
