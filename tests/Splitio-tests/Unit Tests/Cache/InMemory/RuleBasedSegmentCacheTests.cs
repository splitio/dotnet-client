using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Common;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache.InMemory
{
    [TestClass]
    public class RuleBasedSegmentCacheTests
    {
        private InMemoryRuleBasedSegmentCache _segmentCache;
        private EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> _eventsManager;
        private bool SdkUpdateFlag = false;
        private EventMetadata eMetadata = null;
        private InternalEventsTask _internalEventsTask;
        public event EventHandler<EventMetadata> SdkUpdate;
        public event EventHandler<EventMetadata> SdkReady;

        [TestInitialize]
        public void Setup()
        {
            var cache = new ConcurrentDictionary<string, RuleBasedSegment>();
            _eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig(), new EventDelivery<SdkEvent, EventMetadata>());
            _internalEventsTask = new InternalEventsTask(_eventsManager, new SplitQueue<Splitio.Services.EventSource.Workers.SdkEventNotification>());
            _internalEventsTask.Start();
            _segmentCache = new InMemoryRuleBasedSegmentCache(cache, _internalEventsTask);
        }

        [TestMethod]
        public void Get_ShouldReturnSegmentIfExists()
        {
            // Arrange
            var segmentName = "test-segment";
            var segment = new RuleBasedSegment { Name = segmentName };

            _segmentCache.Update(new List<RuleBasedSegment> { segment }, new List<string>(), 10);

            // Act
            var result = _segmentCache.Get(segmentName);

            // Assert
            Assert.AreEqual(segment, result);
        }

        [TestMethod]
        public void Get_ShouldReturnNullIfSegmentDoesNotExist()
        {
            // Arrange
            var segmentName = "non-existent-segment";

            // Act
            var result = _segmentCache.Get(segmentName);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetChangeNumber_ShouldReturnChangeNumber()
        {
            // Arrange
            var changeNumber = 12345;
            _segmentCache.SetChangeNumber(changeNumber);

            // Act
            var result = _segmentCache.GetChangeNumber();

            // Assert
            Assert.AreEqual(changeNumber, result);
        }

        [TestMethod]
        public void Update_ShouldAddAndRemoveSegments()
        {
            // Arrange
            var segmentToAdd = new RuleBasedSegment { Name = "segment-to-add" };
            var segmentToRemove = new RuleBasedSegment { Name = "segment-to-remove" };
            var till = 67890;

            // Act
            _segmentCache.Update(new List<RuleBasedSegment> { segmentToAdd, segmentToRemove }, new List<string> { segmentToRemove.Name }, till);

            // Assert
            Assert.IsTrue(_segmentCache.Contains(new List<string> { segmentToAdd.Name }));
            Assert.IsFalse(_segmentCache.Contains(new List<string> { segmentToRemove.Name }));
            Assert.AreEqual(till, _segmentCache.GetChangeNumber());
        }

        [TestMethod]
        public void Clear_ShouldRemoveAllSegments()
        {
            // Arrange
            var toAdd = new List<RuleBasedSegment>
            {
                new RuleBasedSegment { Name = "segment1" },
                new RuleBasedSegment { Name = "segment2" }
            };
            _segmentCache.Update(toAdd, new List<string>(), 10);

            // Act
            _segmentCache.Clear();

            // Assert
            Assert.IsFalse(_segmentCache.Contains(new List<string> { "segment1", "segment2" }));
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnSegmentIfExists()
        {
            // Arrange
            var segmentName = "test-segment";
            var segment = new RuleBasedSegment { Name = segmentName };
            _segmentCache.Update(new List<RuleBasedSegment> { segment }, new List<string>(), 10);

            // Act
            var result = await _segmentCache.GetAsync(segmentName);

            // Assert
            Assert.AreEqual(segment, result);
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnNullIfSegmentDoesNotExist()
        {
            // Arrange
            var segmentName = "non-existent-segment";

            // Act
            var result = await _segmentCache.GetAsync(segmentName);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Contains_ShouldReturnTrue()
        {
            // Arrange
            var toAdd = new List<RuleBasedSegment>
            {
                new RuleBasedSegment { Name = "segment1" },
                new RuleBasedSegment { Name = "segment2" }
            };

            _segmentCache.Update(toAdd, new List<string>(), 10);

            // Act & Assert
            Assert.IsTrue(_segmentCache.Contains(new List<string> { "segment1" }));
            Assert.IsFalse(_segmentCache.Contains(new List<string> { "segment1", "segment3" }));
            Assert.IsTrue(_segmentCache.Contains(new List<string> { "segment1", "segment2" }));
        }

        [TestMethod]
        public void Update_ShouldNotifyEvent()
        {
            // Arrange
            var segmentToAdd = new RuleBasedSegment { Name = "segment-to-add" };
            var segmentToRemove = new RuleBasedSegment { Name = "segment-to-remove" };
            var till = 67890;
            var toNotify = new List<string> { { "segment-to-add" }, { "segment-to-remove" } };
            SdkUpdate += sdkUpdate_callback;
            _eventsManager.Register(SdkEvent.SdkUpdate, TriggerSdkUpdate);
            _eventsManager.Register(SdkEvent.SdkReady, TriggerSdkReady);
            _eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, null);

            // Act
            SdkUpdateFlag = false;
            _segmentCache.Update(new List<RuleBasedSegment> { segmentToAdd, segmentToRemove }, new List<string> { segmentToRemove.Name }, till);
            SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));

            // Assert
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.SegmentsUpdate, eMetadata.GetEventType());

            // Act
            SdkUpdateFlag = false;
            _segmentCache.Update(new List<RuleBasedSegment>(), new List<string>(), 12345);

            // Assert
            Assert.IsFalse(SdkUpdateFlag);
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
