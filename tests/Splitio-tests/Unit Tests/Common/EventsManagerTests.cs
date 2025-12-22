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
        private bool SdkReady2 = false;
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

            PublicSdkReadyHandler += sdkReady_callback;
            PublicSdkReadyHandler += sdkReady_callback2;
            PublicSdkUpdateHandler += sdkUpdate_callback;
            PublicSdkTimedOutHandler += sdkTimedOut_callback;
            Dictionary<string, object> metaData = new Dictionary<string, object>
            {
                { "flags", new List<string> {{ "flag1" }} }
            };

            eventsManager.Register(SdkEvent.SdkReady, PublicSdkReadyHandler);
            eventsManager.Register(SdkEvent.SdkUpdate, PublicSdkUpdateHandler);

            eventsManager.NotifyInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(metaData));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(metaData));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(metaData));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkUpdate);
            Assert.IsFalse(SdkTimedOut);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOut, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkUpdate);
            Assert.IsFalse(SdkTimedOut); // not fired as it is not registered yet

            eventsManager.Register(SdkEvent.SdkReadyTimeout, PublicSdkTimedOutHandler);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOut, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkUpdate);
            Assert.IsTrue(SdkTimedOut);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkReady, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(SdkReady);
            Assert.IsTrue(SdkReady2);
            Assert.IsFalse(SdkUpdate);
            Assert.IsFalse(SdkTimedOut);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.Register(SdkEvent.SdkReadyTimeout, PublicSdkTimedOutHandler);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOut, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkUpdate);
            Assert.IsFalse(SdkTimedOut); // not fired as suppressed by sdkReady

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdate, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOut);
            Assert.IsFalse(SdkReady);
            Assert.IsTrue(SdkUpdate);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdate, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOut);
            Assert.IsFalse(SdkReady);
            Assert.IsTrue(SdkUpdate);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdate, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOut);
            Assert.IsFalse(SdkReady);
            Assert.IsTrue(SdkUpdate);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdate, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOut);
            Assert.IsFalse(SdkReady);
            Assert.IsTrue(SdkUpdate);
            VerifyMetadata(eMetadata);

            eventsManager.Unregister(SdkEvent.SdkUpdate);
            eventsManager.Unregister(SdkEvent.SdkUpdate); // should not cause exception
            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdate, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOut);
            Assert.IsFalse(SdkReady);
            Assert.IsFalse(SdkUpdate);
        }

        void ResetAllVariables()
        {
            SdkReady = false;
            SdkReady2 = false;
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

        private void sdkReady_callback2(object sender, EventMetadata metadata)
        {
            SdkReady2 = true;
            eMetadata = metadata;
        }

        private void sdkTimedOut_callback(object sender, EventMetadata metadata)
        {
            SdkTimedOut = true;
            eMetadata = metadata;
        }
    }
}
