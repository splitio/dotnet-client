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
        private string FireFirst = "";
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

            eventsManager.NotifyInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(SdkEventType.SegmentsUpdate, new List<string>()));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(SdkEventType.FlagsUpdate, new List<string> { { "flag1" } }));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(SdkEventType.SegmentsUpdate, new List<string>()));
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(SdkEventType.FlagsUpdate, new List<string> { { "flag1" } }));
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsFalse(SdkUpdateFlag);
            Assert.IsFalse(SdkTimedOutFlag);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, null);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsFalse(SdkUpdateFlag);
            Assert.IsFalse(SdkTimedOutFlag); // not fired as it is not registered yet

            eventsManager.Register(SdkEvent.SdkReadyTimeout, TriggerSdkTimeout);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, null);
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOutFlag, TimeSpan.FromMilliseconds(1000));
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsFalse(SdkUpdateFlag);
            Assert.IsTrue(SdkTimedOutFlag);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, null);
            System.Threading.SpinWait.SpinUntil(() => SdkReadyFlag, TimeSpan.FromMilliseconds(1000));
            System.Threading.SpinWait.SpinUntil(() => SdkReadyFlag2, TimeSpan.FromMilliseconds(1000));
            Assert.IsTrue(SdkReadyFlag);
            Assert.IsTrue(SdkReadyFlag2);
            Assert.IsFalse(SdkUpdateFlag);
            Assert.IsFalse(SdkTimedOutFlag);

            ResetAllVariables();
            eventsManager.Register(SdkEvent.SdkReadyTimeout, TriggerSdkTimeout);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, null);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsFalse(SdkUpdateFlag);
            Assert.IsFalse(SdkTimedOutFlag); // not fired as suppressed by sdkReady

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(SdkEventType.FlagsUpdate, new List<string> { { "flag1" } }));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));
            Assert.IsFalse(SdkTimedOutFlag);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.FlagsUpdate, eMetadata.GetEventType());
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(SdkEventType.SegmentsUpdate, new List<string>()));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));
            Assert.IsFalse(SdkTimedOutFlag);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.SegmentsUpdate, eMetadata.GetEventType());

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(SdkEventType.SegmentsUpdate, new List<string>()));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));
            Assert.IsFalse(SdkTimedOutFlag);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.SegmentsUpdate, eMetadata.GetEventType());

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(SdkEventType.FlagsUpdate, new List<string> { { "flag1" } }));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));
            Assert.IsFalse(SdkTimedOutFlag);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.FlagsUpdate, eMetadata.GetEventType());
            VerifyMetadata(eMetadata);

            eventsManager.Unregister(SdkEvent.SdkUpdate);
            eventsManager.Unregister(SdkEvent.SdkUpdate); // should not cause exception
            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(SdkEventType.FlagsUpdate, new List<string> { { "flag1" } }));
            Assert.IsFalse(SdkTimedOutFlag);
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsFalse(SdkUpdateFlag);
        }

        [TestMethod]
        public void TestFireOrderEvents()
        {
            //Act
            EventsManagerConfig config = new EventsManagerConfig();
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(config, new EventDelivery<SdkEvent, EventMetadata>());

            SdkReady += sdkReady_callback;
            SdkTimedOut += sdkTimedOut_callback;

            eventsManager.Register(SdkEvent.SdkReady, TriggerSdkReady);
            eventsManager.Register(SdkEvent.SdkReadyTimeout, TriggerSdkTimeout);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut, null);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, null);
            System.Threading.SpinWait.SpinUntil(() => SdkTimedOutFlag, TimeSpan.FromMilliseconds(2000));
            System.Threading.SpinWait.SpinUntil(() => SdkReadyFlag, TimeSpan.FromMilliseconds(2000));
            Assert.IsTrue(SdkReadyFlag);
            Assert.IsTrue(SdkTimedOutFlag);
            Assert.AreEqual("SdkTimeout", FireFirst);
        }
            void ResetAllVariables()
        {
            SdkReadyFlag = false;
            SdkReadyFlag2 = false;
            SdkTimedOutFlag = false;
            eMetadata = null;
            SdkUpdateFlag = false;
        }

        static void VerifyMetadata(EventMetadata eMetdata)
        {
            Assert.IsTrue(eMetdata.GetNames().Count == 1);
            Assert.IsTrue(eMetdata.GetNames().Contains("flag1"));
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
            if (FireFirst.Equals("")) FireFirst = "SdkReady";
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
            if (FireFirst.Equals("")) FireFirst = "SdkTimeout";
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
