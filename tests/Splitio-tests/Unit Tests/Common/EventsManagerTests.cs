using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using System;
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
        private bool SdkUpdate = false;
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

            eventsManager.PublicSdkReadyHandler += sdkReady_callback;
            eventsManager.PublicSdkUpdateHandler += sdkUpdate_callback;
            eventsManager.PublicSdkTimedOutHandler += sdkTimedOut_callback;

            Dictionary<string, object> metaData = new Dictionary<string, object>
            {
                { "flags", new List<string> {{ "flag1" }} }
            };

            eventsManager.OnSdkInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => RuleBasedSegmentsUpdated, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(RuleBasedSegmentsUpdated);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => FlagKilledNotification, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(FlagKilledNotification);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SdkReady, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkReady, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(SdkReady);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOut, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(SdkTimedOut);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => FlagsUpdated, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(FlagsUpdated);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SegmentsUpdated, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(SegmentsUpdated);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkEvent(SdkEvent.SdkReady, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkReady, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(SdkReady);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkEvent(SdkEvent.SdkUpdate, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdate, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(SdkUpdate);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.OnSdkEvent(SdkEvent.SdkReadyTimeout, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOut, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(SdkTimedOut);
            VerifyMetadata(eMetadata);
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
            Assert.IsFalse(eventsManager.GetSdkEventStatus(SdkEvent.SdkReady));
            Assert.IsFalse(eventsManager.GetSdkEventStatus(SdkEvent.SdkUpdate));
            Assert.IsFalse(eventsManager.GetSdkEventStatus(SdkEvent.SdkReadyTimeout));

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

            eventsManager.UpdateSdkEventStatus(SdkEvent.SdkReady, true);
            Assert.IsTrue(eventsManager.GetSdkEventStatus(SdkEvent.SdkReady));

            eventsManager.UpdateSdkEventStatus(SdkEvent.SdkUpdate, true);
            Assert.IsTrue(eventsManager.GetSdkEventStatus(SdkEvent.SdkUpdate));

            eventsManager.UpdateSdkEventStatus(SdkEvent.SdkReadyTimeout, true);
            Assert.IsTrue(eventsManager.GetSdkEventStatus(SdkEvent.SdkReadyTimeout));
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
            SdkUpdate = false;
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
