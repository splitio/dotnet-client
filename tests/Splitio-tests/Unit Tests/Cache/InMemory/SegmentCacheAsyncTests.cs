using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using WireMock.Pact.Models.V2;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class SegmentCacheAsyncTests
    {
        private readonly ISegmentCache _cache;
        private EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> _eventsManager;
        private bool SdkUpdate = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> PublicSdkUpdateHandler;

        public SegmentCacheAsyncTests()
        {
            var segments = new ConcurrentDictionary<string, Segment>();
            _eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig());
            _cache = new InMemorySegmentCache(segments, _eventsManager);
        }

        [TestMethod]
        public async Task IsInSegmentAsyncTestFalse()
        {
            //Arrange
            var segmentName = "segment_test";

            //Act
            var result = await _cache.IsInSegmentAsync(segmentName, "abcd");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsInSegmentAsyncTestTrue()
        {
            //Arrange
            var segmentName = "segment_test";

            _cache.AddToSegment(segmentName, new List<string> { "abcd", "zzzzf" });

            //Act
            var result = await _cache.IsInSegmentAsync(segmentName, "abcd");

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task NotifyEventsTest()
        {
            //Arrange
            var segmentName = "segment_test";
            Splitio.Util.Helper.BuildInternalSdkEventStatus(_eventsManager);
            var toNotify = new List<string> { { segmentName } };
            PublicSdkUpdateHandler += sdkUpdate_callback;
            _eventsManager.Register(SdkEvent.SdkUpdate, sdkUpdate_callback);
            _eventsManager.Register(SdkEvent.SdkReady, sdkUpdate_callback);
            _eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, new EventMetadata(new Dictionary<string, object>()),
                Splitio.Util.Helper.GetSdkEventIfApplicable(SdkInternalEvent.SdkReady, _eventsManager));

            //Act
            SdkUpdate = false;
            _cache.AddToSegment(segmentName, new List<string> { "abcd", "zzzzf" });

            //Assert
            Assert.IsTrue(SdkUpdate);
            Assert.IsTrue(eMetadata.ContainKey(Splitio.Constants.EventMetadataKeys.Segments));
            string segment = (string)eMetadata.GetData()[Splitio.Constants.EventMetadataKeys.Segments];
            Assert.AreEqual(segmentName, segment);
        }

        private void sdkUpdate_callback(object sender, EventMetadata metadata)
        {
            SdkUpdate = true;
            eMetadata = metadata;
        }
    }
}
