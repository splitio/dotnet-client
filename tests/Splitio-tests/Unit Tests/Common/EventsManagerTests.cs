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
        private bool SdkUpdateFlag = false;
        private string FireFirst = "";
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> SdkReady;
        public event EventHandler<EventMetadata> SdkUpdate;

        [TestMethod]
        public void TestFiringEvents()
        {
            //Act
            EventsManagerConfig config = new EventsManagerConfig();
            EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(config, new EventDelivery<SdkEvent, EventMetadata>());

            SdkReady += sdkReady_callback;
            SdkReady += sdkReady_callback2;
            SdkUpdate += sdkUpdate_callback;
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

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, null);
            System.Threading.SpinWait.SpinUntil(() => SdkReadyFlag, TimeSpan.FromMilliseconds(1000));
            System.Threading.SpinWait.SpinUntil(() => SdkReadyFlag2, TimeSpan.FromMilliseconds(1000));
            Assert.IsTrue(SdkReadyFlag);
            Assert.IsTrue(SdkReadyFlag2);
            Assert.IsFalse(SdkUpdateFlag);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagKilledNotification, new EventMetadata(SdkEventType.FlagsUpdate, new List<string> { { "flag1" } }));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.FlagsUpdate, eMetadata.GetEventType());
            VerifyMetadata(eMetadata);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SegmentsUpdated, new EventMetadata(SdkEventType.SegmentsUpdate, new List<string>()));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.SegmentsUpdate, eMetadata.GetEventType());

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated, new EventMetadata(SdkEventType.SegmentsUpdate, new List<string>()));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.SegmentsUpdate, eMetadata.GetEventType());

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(SdkEventType.FlagsUpdate, new List<string> { { "flag1" } }));
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));
            Assert.IsFalse(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.FlagsUpdate, eMetadata.GetEventType());
            VerifyMetadata(eMetadata);

            eventsManager.Unregister(SdkEvent.SdkUpdate);
            eventsManager.Unregister(SdkEvent.SdkUpdate); // should not cause exception
            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, new EventMetadata(SdkEventType.FlagsUpdate, new List<string> { { "flag1" } }));
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
            SdkUpdate += sdkUpdate_callback;

            eventsManager.Register(SdkEvent.SdkReady, TriggerSdkReady);
            eventsManager.Register(SdkEvent.SdkUpdate, TriggerSdkUpdate);

            ResetAllVariables();
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, null);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.FlagsUpdated, null);
            System.Threading.SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(2000));
            System.Threading.SpinWait.SpinUntil(() => SdkReadyFlag, TimeSpan.FromMilliseconds(2000));
            Assert.IsTrue(SdkReadyFlag);
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual("SdkReady", FireFirst);
        }

        void ResetAllVariables()
        {
            SdkReadyFlag = false;
            SdkReadyFlag2 = false;
            eMetadata = null;
            SdkUpdateFlag = false;
            FireFirst = "";
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
            if (FireFirst.Equals("")) FireFirst = "SdkUpdate";
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

        private void TriggerSdkReady(EventMetadata metaData)
        {
            SdkReady?.Invoke(this, metaData);
        }

        private void TriggerSdkUpdate(EventMetadata metaData)
        {
            SdkUpdate?.Invoke(this, metaData);
        }
    }
}
