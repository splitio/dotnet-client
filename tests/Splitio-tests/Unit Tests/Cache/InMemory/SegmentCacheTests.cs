using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class SegmentCacheTests
    {
        private bool SdkUpdate = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> PublicSdkUpdateHandler;

        [TestMethod]
        public void RegisterSegmentTest()
        {
            //Arrange
            var eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig());
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), eventsManager);
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
            var eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig());
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), eventsManager);
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
            var eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig());
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), eventsManager);

            //Act
            var result = segmentCache.IsInSegment("test", "abcd");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RemoveKeyFromSegmentTest()
        {
            //Arrange
            var eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig());
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), eventsManager);
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
            var eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig());
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), eventsManager);
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
            var eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig());
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), eventsManager);
            var keys = new List<string> { "1234" };
            var segmentName = "test";
            var toNotify = new List<string> { { segmentName } };
            PublicSdkUpdateHandler += sdkUpdate_callback;
            eventsManager.Register(SdkEvent.SdkUpdate, sdkUpdate_callback);
            eventsManager.Register(SdkEvent.SdkReady, sdkUpdate_callback);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, new EventMetadata(new Dictionary<string, object>()));

            // Act
            SdkUpdate = false;
            segmentCache.AddToSegment(segmentName, keys);

            //Assert
            Assert.IsTrue(SdkUpdate);
            Assert.IsTrue(eMetadata.ContainKey(Splitio.Constants.EventMetadataKeys.Segments));
            string segment = (string) eMetadata.GetData()[Splitio.Constants.EventMetadataKeys.Segments];
            Assert.AreEqual(segmentName, segment);

            // Act
            SdkUpdate = false;
            eMetadata = null;
            segmentCache.RemoveFromSegment(segmentName, keys);

            //Assert
            Assert.IsTrue(SdkUpdate);
            Assert.IsTrue(eMetadata.ContainKey(Splitio.Constants.EventMetadataKeys.Segments));
            segment = (string)eMetadata.GetData()[Splitio.Constants.EventMetadataKeys.Segments];
            Assert.AreEqual(segmentName, segment);
        }

        private void sdkUpdate_callback(object sender, EventMetadata metadata)
        {
            SdkUpdate = true;
            eMetadata = metadata;
        }
    }
}
