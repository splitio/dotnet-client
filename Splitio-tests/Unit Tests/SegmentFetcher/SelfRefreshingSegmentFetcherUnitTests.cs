using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Client.Classes;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.SplitFetcher.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.SegmentFetcher
{
    [TestClass]
    public class SelfRefreshingSegmentFetcherUnitTests
    {
        private readonly IWrapperAdapter wrapperAdapter = WrapperAdapter.Instance();

        private static readonly string PayedSplitJson = @"{'name': 'payed','added': ['abcdz','bcadz','xzydz'],'removed': [],'since': -1,'till': 10001}";

        [TestMethod]
        public async Task InitializeSegmentNotExistent()
        {
            // Arrange
            var gates = new InMemoryReadinessGatesCache();
            gates.SetReady();
            var apiClient = new Mock<ISegmentSdkApiClient>();            
            var apiFetcher = new ApiSegmentChangeFetcher(apiClient.Object);
            var segments = new ConcurrentDictionary<string, Segment>();
            var cache = new InMemorySegmentCache(segments);
            var segmentTaskQueue = new SegmentTaskQueue();
            var segmentFetcher = new SelfRefreshingSegmentFetcher(apiFetcher, gates, 1, cache, 1, segmentTaskQueue, new TasksManager(wrapperAdapter), wrapperAdapter);
            segmentFetcher.Start();

            apiClient
                .Setup(x => x.FetchSegmentChanges(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<FetchOptions>()))
                .Returns(Task.FromResult(PayedSplitJson));

            // Act
            segmentFetcher.InitializeSegment("payed");

            // Assert
            Thread.Sleep(5000);
            Assert.IsTrue(await cache.IsInSegmentAsync("payed", "abcdz"));
        }

        [TestMethod]
        public void StartSchedullerSuccessfully()
        {
            // Arrange
            var statusManager = new Mock<IStatusManager>();
            var apiClient = new Mock<ISegmentSdkApiClient>();         
            var apiFetcher = new ApiSegmentChangeFetcher(apiClient.Object);
            var segments = new ConcurrentDictionary<string, Segment>();
            var cache = new InMemorySegmentCache(segments);
            var segmentTaskQueue = new SegmentTaskQueue();
            var segmentFetcher = new SelfRefreshingSegmentFetcher(apiFetcher, statusManager.Object, 10, cache, 1, segmentTaskQueue, new TasksManager(wrapperAdapter), wrapperAdapter);

            apiClient
                .Setup(x => x.FetchSegmentChanges(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<FetchOptions>()))
                .Returns(Task.FromResult(PayedSplitJson));

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
            var statusManager = new Mock<IStatusManager>();
            var apiFetcher = new Mock<ISegmentChangeFetcher>();
            var cache = new Mock<ISegmentCache>();
            var segmentTaskQueue = new Mock<ISegmentTaskQueue>();
            var segmentFetcher = new SelfRefreshingSegmentFetcher(apiFetcher.Object, statusManager.Object, 10, cache.Object, 1, segmentTaskQueue.Object, new TasksManager(wrapperAdapter), wrapperAdapter);
            var segment1 = "segment-1";
            var segment2 = "segment-2";
            var segment3 = "segment-3";

            cache.Setup(mock => mock.GetChangeNumberAsync(segment1)).ReturnsAsync(-1);
            cache.Setup(mock => mock.GetChangeNumberAsync(segment2)).ReturnsAsync(30);
            cache.Setup(mock => mock.GetChangeNumberAsync(segment3)).ReturnsAsync(-1);

            // Act
            await segmentFetcher.FetchSegmentsIfNotExists(new List<string> { segment1, segment2, segment3, segment2, segment3, segment3, segment3 });

            // Assert
            cache.Verify(mock => mock.GetChangeNumberAsync(segment1), Times.Exactly(2));
            cache.Verify(mock => mock.GetChangeNumberAsync(segment2), Times.Once);
            cache.Verify(mock => mock.GetChangeNumberAsync(segment3), Times.Exactly(2));

            apiFetcher.Verify(mock => mock.Fetch(segment1, -1, It.IsAny<FetchOptions>()), Times.Once);
            apiFetcher.Verify(mock => mock.Fetch(segment3, -1, It.IsAny<FetchOptions>()), Times.Once);
            apiFetcher.Verify(mock => mock.Fetch(segment2, -1, It.IsAny<FetchOptions>()), Times.Never);
            apiFetcher.Verify(mock => mock.Fetch(segment2, 30, It.IsAny<FetchOptions>()), Times.Never);
        }
    }
}
