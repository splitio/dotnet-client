using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using System;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class EventsManagerTests
    {
        private bool SdkReady = false;
        private bool SdkTimedOut = false;
        private bool SdkUpdate = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> PublicSdkReadyHandler;
        public event EventHandler<EventMetadata> PublicSdkUpdateHandler;
        public event EventHandler<EventMetadata> PublicSdkTimedOutHandler;

        [TestMethod]
        public void TestFiringEvents()
        {
            //Act
            EventsManagerConfig config = new EventsManagerConfig();
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(config);
            Splitio.Util.Helper.BuildInternalSdkEventStatus(eventsManager);

            PublicSdkReadyHandler += sdkReady_callback;
            PublicSdkUpdateHandler += sdkUpdate_callback;
            PublicSdkTimedOutHandler += sdkTimedOut_callback;

            Dictionary<string, object> metaData = new Dictionary<string, object>
            {
                { "flags", new List<string> {{ "flag1" }} }
            };

            eventsManager.Register(SdkEvent.SdkReady, sdkReady_callback);
            eventsManager.Register(SdkEvent.SdkUpdate, sdkUpdate_callback);

            eventsManager.NotifyInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.RuleBasedSegmentsUpdated,
                    eventsManager));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.FlagKilledNotification,
                    eventsManager));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.SegmentsUpdated,
                    eventsManager));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.FlagsUpdated,
                    eventsManager));
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkUpdate);
            Assert.IsFalse(SdkTimedOut);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.SdkTimedOut,
                    eventsManager));
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOut, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkUpdate);
            Assert.IsFalse(SdkTimedOut); // not fired as it is not registered yet

            eventsManager.Register(SdkEvent.SdkReadyTimeout, sdkTimedOut_callback);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.SdkTimedOut,
                    eventsManager));
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOut, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkUpdate);
            Assert.IsTrue(SdkTimedOut);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            List<SdkEvent> eventsToNotify = Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.SdkReady,
                    eventsManager);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, new EventMetadata(metaData),
                eventsToNotify);
            System.Threading.SpinWait.SpinUntil(() => SdkReady, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(SdkReady);
            Assert.IsFalse(SdkUpdate);
            Assert.IsFalse(SdkTimedOut);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.Register(SdkEvent.SdkReadyTimeout, sdkTimedOut_callback);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.SdkTimedOut,
                    eventsManager));
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOut, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkUpdate);
            Assert.IsFalse(SdkTimedOut); // not fired as suppressed by sdkReady

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.FlagKilledNotification,
                    eventsManager));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdate, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOut);
            Assert.IsFalse(SdkReady);
            Assert.IsTrue(SdkUpdate);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.SegmentsUpdated,
                    eventsManager));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdate, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOut);
            Assert.IsFalse(SdkReady);
            Assert.IsTrue(SdkUpdate);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.RuleBasedSegmentsUpdated,
                    eventsManager));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdate, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOut);
            Assert.IsFalse(SdkReady);
            Assert.IsTrue(SdkUpdate);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.FlagsUpdated,
                    eventsManager));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdate, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOut);
            Assert.IsFalse(SdkReady);
            Assert.IsTrue(SdkUpdate);
            VerifyMetadata(eMetadata);

            eventsManager.Unregister(SdkEvent.SdkUpdate);
            eventsManager.Unregister(SdkEvent.SdkUpdate); // should not cause exception
            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.FlagsUpdated,
                    eventsManager));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdate, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOut);
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkUpdate);
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
            Assert.IsTrue(eMetadata.ContainKey(Splitio.Constants.EventMetadataKeys.Flags));
            List<string> flags = (List<string>)eMetadata.GetData()[Splitio.Constants.EventMetadataKeys.Flags];
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
