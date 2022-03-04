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
using Splitio.Services.Shared.Classes;

namespace Splitio_Tests.Unit_Tests.SegmentFetcher
{
    [TestClass]
    public class SelfRefreshingSegmentFetcherUnitTests
    {
        private readonly WrapperAdapter wrapperAdapter = new WrapperAdapter();

        private static readonly string PayedSplitJson = @"{'name': 'payed','added': ['abcdz','bcadz','xzydz'],'removed': [],'since': -1,'till': 10001}";

        [TestMethod]
        public void InitializeSegmentNotExistent()
        {
            // Arrange
            var apiClient = new Mock<ISegmentSdkApiClient>();
            var statusManager = new InMemoryReadinessGatesCache();
            statusManager.SetReady();

            var config = new SegmentFetcherConfig
            {
                Interval = 1,
                NumberOfParallelSegments = 1,
                SegmentChangeFetcher = new ApiSegmentChangeFetcher(apiClient.Object),
                SegmentsCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>()),
                SegmentTaskQueue = new SegmentTaskQueue(),
                StatusManager = statusManager,
                TasksManager = new TasksManager(wrapperAdapter),
                WrapperAdapter = wrapperAdapter
            };

            var segmentFetcher = new SelfRefreshingSegmentFetcher(config);
            segmentFetcher.Start();

            apiClient
                .Setup(x => x.FetchSegmentChanges(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<FetchOptions>()))
                .Returns(Task.FromResult(PayedSplitJson));

            // Act
            segmentFetcher.InitializeSegment("payed");

            // Assert
            Thread.Sleep(5000);
            Assert.IsTrue(config.SegmentsCache.IsInSegment("payed", "abcdz"));
        }

        [TestMethod]
        public void StartSchedullerSuccessfully()
        {
            // Arrange
            var apiClient = new Mock<ISegmentSdkApiClient>();

            var config = new SegmentFetcherConfig
            {
                Interval = 10,
                NumberOfParallelSegments = 1,
                SegmentChangeFetcher = new ApiSegmentChangeFetcher(apiClient.Object),
                SegmentsCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>()),
                SegmentTaskQueue = new SegmentTaskQueue(),
                StatusManager = new Mock<IStatusManager>().Object,
                TasksManager = new TasksManager(wrapperAdapter),
                WrapperAdapter = wrapperAdapter
            };

            var segmentFetcher = new SelfRefreshingSegmentFetcher(config);

            apiClient
                .Setup(x => x.FetchSegmentChanges(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<FetchOptions>()))
                .Returns(Task.FromResult(PayedSplitJson));

            // Act
            segmentFetcher.InitializeSegment("payed");
            segmentFetcher.Start();

            // Assert
            Assert.IsTrue(config.SegmentTaskQueue.GetQueue().TryTake(out SelfRefreshingSegment segment, -1));
        }

        [TestMethod]
        public async Task FetchSegmentsIfNotExists()
        {
            // Arrange
            var segmentChangeFetcher = new Mock<ISegmentChangeFetcher>();
            var segmentsCache = new Mock<ISegmentCache>();

            var config = new SegmentFetcherConfig
            {
                Interval = 10,
                NumberOfParallelSegments = 1,
                SegmentChangeFetcher = segmentChangeFetcher.Object,
                SegmentsCache = segmentsCache.Object,
                SegmentTaskQueue = new Mock<ISegmentTaskQueue>().Object,
                StatusManager = new Mock<IStatusManager>().Object,
                TasksManager = new TasksManager(wrapperAdapter),
                WrapperAdapter = wrapperAdapter
            };

            var segmentFetcher = new SelfRefreshingSegmentFetcher(config);
            var segment1 = "segment-1";
            var segment2 = "segment-2";
            var segment3 = "segment-3";

            segmentsCache.Setup(mock => mock.GetChangeNumber(segment1)).Returns(-1);
            segmentsCache.Setup(mock => mock.GetChangeNumber(segment2)).Returns(30);
            segmentsCache.Setup(mock => mock.GetChangeNumber(segment3)).Returns(-1);

            // Act
            await segmentFetcher.FetchSegmentsIfNotExists(new List<string> { segment1, segment2, segment3, segment2, segment3, segment3, segment3 });

            // Assert
            segmentsCache.Verify(mock => mock.GetChangeNumber(segment1), Times.Exactly(2));
            segmentsCache.Verify(mock => mock.GetChangeNumber(segment2), Times.Once);
            segmentsCache.Verify(mock => mock.GetChangeNumber(segment3), Times.Exactly(2));

            segmentChangeFetcher.Verify(mock => mock.Fetch(segment1, -1, It.IsAny<FetchOptions>()), Times.Once);
            segmentChangeFetcher.Verify(mock => mock.Fetch(segment3, -1, It.IsAny<FetchOptions>()), Times.Once);
            segmentChangeFetcher.Verify(mock => mock.Fetch(segment2, -1, It.IsAny<FetchOptions>()), Times.Never);
            segmentChangeFetcher.Verify(mock => mock.Fetch(segment2, 30, It.IsAny<FetchOptions>()), Times.Never);
        }
    }
}
