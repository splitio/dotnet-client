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
        private bool SdkReadyFlag = false;
        private bool SdkReadyFlag2 = false;
        private bool SdkTimedOutFlag = false;
        private bool SdkUpdateFlag = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> SdkReady;
        public event EventHandler<EventMetadata> SdkUpdate;
        public event EventHandler<EventMetadata> SdkTimedOut;

        [TestMethod]
        public void TestFiringEvents()
        {
            //Act
            EventsManagerConfig config = new EventsManagerConfig();
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(config, new EventDelivery<SdkEvent, EventMetadata>());

            SdkReady += sdkReady_callback;
            SdkReady += sdkReady_callback2;
            SdkUpdate += sdkUpdate_callback;
            SdkTimedOut += sdkTimedOut_callback;
            Dictionary<string, object> metaData = new Dictionary<string, object>
            {
                { "flags", new List<string> {{ "flag1" }} }
            };

            eventsManager.Register(SdkEvent.SdkReady, TriggerSdkReady);
            eventsManager.Register(SdkEvent.SdkUpdate, TriggerSdkUpdate);

            eventsManager.NotifyInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(metaData));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(metaData));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(metaData));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsFalse(SdkUpdateFlag);
            Assert.IsFalse(SdkTimedOutFlag);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOutFlag, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsFalse(SdkUpdateFlag);
            Assert.IsFalse(SdkTimedOutFlag); // not fired as it is not registered yet

            eventsManager.Register(SdkEvent.SdkReadyTimeout, TriggerSdkTimeout);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOutFlag, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsFalse(SdkUpdateFlag);
            Assert.IsTrue(SdkTimedOutFlag);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkReadyFlag, TimeSpan.FromMilliseconds(500));
            Assert.IsTrue(SdkReadyFlag);
            Assert.IsTrue(SdkReadyFlag2);
            Assert.IsFalse(SdkUpdateFlag);
            Assert.IsFalse(SdkTimedOutFlag);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.Register(SdkEvent.SdkReadyTimeout, TriggerSdkTimeout);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOutFlag, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsFalse(SdkUpdateFlag);
            Assert.IsFalse(SdkTimedOutFlag); // not fired as suppressed by sdkReady

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOutFlag);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOutFlag);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOutFlag);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOutFlag);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            VerifyMetadata(eMetadata);

            eventsManager.Unregister(SdkEvent.SdkUpdate);
            eventsManager.Unregister(SdkEvent.SdkUpdate); // should not cause exception
            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(metaData));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(SdkTimedOutFlag);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsFalse(SdkUpdateFlag);
        }

        void ResetAllVariables()
        {
            SdkReadyFlag = false;
            SdkReadyFlag2 = false;
            SdkTimedOutFlag = false;
            eMetadata = null;
            SdkUpdateFlag = false;
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
            SdkUpdateFlag = true;
            eMetadata = metadata;
        }

        private void sdkReady_callback(object sender, EventMetadata metadata)
        {
            SdkReadyFlag = true;
            eMetadata = metadata;
        }

        private void sdkReady_callback2(object sender, EventMetadata metadata)
        {
            SdkReadyFlag2 = true;
            eMetadata = metadata;
        }

        private void sdkTimedOut_callback(object sender, EventMetadata metadata)
        {
            SdkTimedOutFlag = true;
            eMetadata = metadata;
        }

        private void TriggerSdkReady(EventMetadata metaData)
        {
            SdkReady?.Invoke(this, metaData);
        }

        private void TriggerSdkUpdate(EventMetadata metaData)
        {
            SdkUpdate?.Invoke(this, metaData);
        }

        private void TriggerSdkTimeout(EventMetadata metaData)
        {
            SdkTimedOut?.Invoke(this, metaData);
        }

    }
}
