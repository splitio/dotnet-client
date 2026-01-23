using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Common;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class SegmentCacheTests
    {
        private bool SdkUpdateFlag = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> SdkUpdate;
        public event EventHandler<EventMetadata> SdkReady;

        [TestMethod]
        public void RegisterSegmentTest()
        {
            //Arrange
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);
            var keys = new List<string> { "abcd", "1234" };
            var segmentName = "test";

            //Act
            segmentCache.AddToSegment(segmentName, keys);
            var result = segmentCache.IsInSegment(segmentName, "abcd");
            
            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsNotInSegmentTest()
        {
            //Arrange
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);
            var keys = new List<string> { "1234" };
            var segmentName = "test";

            //Act
            segmentCache.AddToSegment(segmentName, keys);
            var result = segmentCache.IsInSegment(segmentName, "abcd");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsInSegmentWithInexistentSegmentTest()
        {
            //Arrange
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);

            //Act
            var result = segmentCache.IsInSegment("test", "abcd");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RemoveKeyFromSegmentTest()
        {
            //Arrange
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);
            var keys = new List<string> { "1234" };
            var segmentName = "test";

            //Act
            segmentCache.AddToSegment(segmentName, keys);
            var result = segmentCache.IsInSegment(segmentName, "1234");
            segmentCache.RemoveFromSegment(segmentName, keys);
            var result2 = segmentCache.IsInSegment(segmentName, "1234");

            //Assert
            Assert.IsTrue(result);
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public void SetAndGetChangeNumberTest()
        {
            //Arrange
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);
            var segmentName = "test";

            //Act
            segmentCache.AddToSegment(segmentName, null);
            segmentCache.SetChangeNumber(segmentName, 1234);
            var result = segmentCache.GetChangeNumber(segmentName);

            //Assert
            Assert.AreEqual(1234, result);
        }

        [TestMethod]
        public void NotifyEventsTest()
        {
            //Arrange
            var eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig(), new EventDelivery<SdkEvent, EventMetadata>());
            var internalEventsTask = new InternalEventsTask(eventsManager, new SplitQueue<Splitio.Services.EventSource.Workers.SdkEventNotification>());
            internalEventsTask.Start();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask);
            var keys = new List<string> { "1234" };
            var segmentName = "test";
            var toNotify = new List<string> { { segmentName } };
            SdkUpdate += sdkUpdate_callback;
            eventsManager.Register(SdkEvent.SdkUpdate, TriggerSdkUpdate);
            eventsManager.Register(SdkEvent.SdkReady, TriggerSdkReady);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, null);

            // Act
            SdkUpdateFlag = false;
            segmentCache.AddToSegment(segmentName, keys);
            SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(2000));

            //Assert
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.SegmentsUpdate, eMetadata.GetEventType());

            // Act
            SdkUpdateFlag = false;
            eMetadata = null;
            segmentCache.RemoveFromSegment(segmentName, keys);
            SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(2000));

            //Assert
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.SegmentsUpdate, eMetadata.GetEventType());
        }

        private void sdkUpdate_callback(object sender, EventMetadata metadata)
        {
            SdkUpdateFlag = true;
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
