using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Client.Classes;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Services.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
            gates.SetReady();
            var apiClient = new Mock<ISegmentSdkApiClient>();            
            var apiFetcher = new ApiSegmentChangeFetcher(apiClient.Object);
            var segments = new ConcurrentDictionary<string, Segment>();
            var cache = new InMemorySegmentCache(segments);
            var segmentTaskQueue = new BlockingCollection<SelfRefreshingSegment>(new ConcurrentQueue<SelfRefreshingSegment>());
            var taskManager = new TasksManager();
            var workerTask = taskManager.NewPeriodicTask(gates, Splitio.Enums.Task.SegmentsWorkerFetcher, 0);
            var worker = new SegmentTaskWorker(5, segmentTaskQueue, gates, workerTask);
            var segmentsTask = taskManager.NewPeriodicTask(gates, Splitio.Enums.Task.SegmentsFetcher, 10);
            var segmentFetcher = new SelfRefreshingSegmentFetcher(apiFetcher, cache, segmentTaskQueue, segmentsTask, worker, gates);
            segmentFetcher.Start();

            apiClient
                .Setup(x => x.FetchSegmentChangesAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<FetchOptions>()))
                .ReturnsAsync(PayedSplitJson);

            // Act
            segmentFetcher.InitializeSegment("payed");

            // Assert
            Thread.Sleep(1000);
            Assert.IsTrue(cache.IsInSegment("payed", "abcdz"));
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
            var segmentTaskQueue = new BlockingCollection<SelfRefreshingSegment>(new ConcurrentQueue<SelfRefreshingSegment>());
            var segmentsTask = new Mock<ISplitTask>();
            var worker = new Mock<IPeriodicTask>();
            var segmentFetcher = new SelfRefreshingSegmentFetcher(apiFetcher, cache, segmentTaskQueue, segmentsTask.Object, worker.Object, statusManager.Object);

            apiClient
                .Setup(x => x.FetchSegmentChangesAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<FetchOptions>()))
                .Returns(Task.FromResult(PayedSplitJson));

            // Act
            segmentFetcher.InitializeSegment("payed");
            segmentFetcher.Start();

            // Assert
            Assert.IsTrue(segmentTaskQueue.TryTake(out SelfRefreshingSegment segment, -1));
        }

        [TestMethod]
        public async Task FetchSegmentsIfNotExists()
        {
            // Arrange
            var statusManager = new Mock<IStatusManager>();
            var apiFetcher = new Mock<ISegmentChangeFetcher>();
            var cache = new Mock<ISegmentCache>();
            var segmentTaskQueue = new Mock<BlockingCollection<SelfRefreshingSegment>>();
            var segmentsTask = new Mock<ISplitTask>();
            var worker = new Mock<IPeriodicTask>();
            var segmentFetcher = new SelfRefreshingSegmentFetcher(apiFetcher.Object, cache.Object, segmentTaskQueue.Object, segmentsTask.Object, worker.Object, statusManager.Object);
            var segment1 = "segment-1";
            var segment2 = "segment-2";
            var segment3 = "segment-3";

            cache.Setup(mock => mock.GetChangeNumber(segment1)).Returns(-1);
            cache.Setup(mock => mock.GetChangeNumber(segment2)).Returns(30);
            cache.Setup(mock => mock.GetChangeNumber(segment3)).Returns(-1);

            // Act
            await segmentFetcher.FetchSegmentsIfNotExistsAsync(new List<string> { segment1, segment2, segment3, segment2, segment3, segment3, segment3 });

            // Assert
            cache.Verify(mock => mock.GetChangeNumber(segment1), Times.Exactly(2));
            cache.Verify(mock => mock.GetChangeNumber(segment2), Times.Once);
            cache.Verify(mock => mock.GetChangeNumber(segment3), Times.Exactly(2));

            apiFetcher.Verify(mock => mock.FetchAsync(segment1, -1, It.IsAny<FetchOptions>()), Times.Once);
            apiFetcher.Verify(mock => mock.FetchAsync(segment3, -1, It.IsAny<FetchOptions>()), Times.Once);
            apiFetcher.Verify(mock => mock.FetchAsync(segment2, -1, It.IsAny<FetchOptions>()), Times.Never);
            apiFetcher.Verify(mock => mock.FetchAsync(segment2, 30, It.IsAny<FetchOptions>()), Times.Never);
        }
    }
}
