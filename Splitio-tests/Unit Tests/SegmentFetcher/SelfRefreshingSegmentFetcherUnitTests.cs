using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.Client.Classes;
using System.Collections.Concurrent;
using Splitio.Services.Cache.Classes;
using Splitio.Domain;
using Moq;
using Splitio.Services.SplitFetcher.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Splitio.Services.Cache.Interfaces;
using System.Collections.Generic;
using Splitio.Services.SegmentFetcher.Interfaces;

namespace Splitio_Tests.Unit_Tests.SegmentFetcher
{
    [TestClass]
    public class SelfRefreshingSegmentFetcherUnitTests
    {
        private static readonly string PayedSplitJson = @"{'name': 'payed','added': ['abcdz','bcadz','xzydz'],'removed': [],'since': -1,'till': 10001}";

        [TestMethod]
        public void InitializeSegmentNotExistent()
        {
            // Arrange
            var gates = new InMemoryReadinessGatesCache();
            var apiClient = new Mock<ISegmentSdkApiClient>();            
            var apiFetcher = new ApiSegmentChangeFetcher(apiClient.Object);
            var segments = new ConcurrentDictionary<string, Segment>();
            var cache = new InMemorySegmentCache(segments);
            var segmentTaskQueue = new SegmentTaskQueue();
            var segmentFetcher = new SelfRefreshingSegmentFetcher(apiFetcher, gates, 10, cache, 1, segmentTaskQueue);
            segmentFetcher.Start();

            apiClient
                .Setup(x => x.FetchSegmentChanges(It.IsAny<string>(), It.IsAny<long>(), false))
                .Returns(Task.FromResult(PayedSplitJson));

            // Act
            segmentFetcher.InitializeSegment("payed");

            // Assert
            Thread.Sleep(5000);
            Assert.IsTrue(gates.AreSegmentsReady(1));
            Assert.IsTrue(cache.IsInSegment("payed", "abcdz"));
        }

        [TestMethod]
        public void StartSchedullerSuccessfully()
        {
            // Arrange
            var gates = new Mock<IReadinessGatesCache>();
            var apiClient = new Mock<ISegmentSdkApiClient>();         
            var apiFetcher = new ApiSegmentChangeFetcher(apiClient.Object);
            var segments = new ConcurrentDictionary<string, Segment>();
            var cache = new InMemorySegmentCache(segments);
            var segmentTaskQueue = new SegmentTaskQueue();
            var segmentFetcher = new SelfRefreshingSegmentFetcher(apiFetcher, gates.Object, 10, cache, 1, segmentTaskQueue);

            apiClient
                .Setup(x => x.FetchSegmentChanges(It.IsAny<string>(), It.IsAny<long>(), false))
                .Returns(Task.FromResult(PayedSplitJson));

            gates
                .Setup(mock => mock.AreSplitsReady(0))
                .Returns(true);

            // Act
            segmentFetcher.InitializeSegment("payed");
            segmentFetcher.Start();

            // Assert
            Assert.IsTrue(segmentTaskQueue.GetQueue().TryTake(out SelfRefreshingSegment segment, -1));
        }

        [TestMethod]
        public async Task FetchSegmentsIfNotExists()
        {
            // Arrange            
            var gates = new Mock<IReadinessGatesCache>();
            var apiFetcher = new Mock<ISegmentChangeFetcher>();
            var cache = new Mock<ISegmentCache>();
            var segmentTaskQueue = new Mock<ISegmentTaskQueue>();
            var segmentFetcher = new SelfRefreshingSegmentFetcher(apiFetcher.Object, gates.Object, 10, cache.Object, 1, segmentTaskQueue.Object);
            var segment1 = "segment-1";
            var segment2 = "segment-2";
            var segment3 = "segment-3";

            cache.Setup(mock => mock.GetChangeNumber(segment1)).Returns(-1);
            cache.Setup(mock => mock.GetChangeNumber(segment2)).Returns(30);
            cache.Setup(mock => mock.GetChangeNumber(segment3)).Returns(-1);

            // Act
            await segmentFetcher.FetchSegmentsIfNotExists(new List<string> { segment1, segment2, segment3, segment2, segment3, segment3, segment3 });

            // Assert
            cache.Verify(mock => mock.GetChangeNumber(segment1), Times.Exactly(2));
            cache.Verify(mock => mock.GetChangeNumber(segment2), Times.Once);
            cache.Verify(mock => mock.GetChangeNumber(segment3), Times.Exactly(2));

            apiFetcher.Verify(mock => mock.Fetch(segment1, -1, false), Times.Once);
            apiFetcher.Verify(mock => mock.Fetch(segment3, -1, false), Times.Once);
            apiFetcher.Verify(mock => mock.Fetch(segment2, -1, false), Times.Never);
            apiFetcher.Verify(mock => mock.Fetch(segment2, 30, false), Times.Never);
        }
    }
}
