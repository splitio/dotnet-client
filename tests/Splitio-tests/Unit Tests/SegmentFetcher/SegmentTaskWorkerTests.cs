using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.SegmentFetcher
{
    [TestClass]
    public class SegmentTaskWorkerTests
    {
        private readonly Mock<ISegmentChangeFetcher> _segmentChangeFetcher;
        private readonly Mock<ISegmentCache> _segmentCache;
        private readonly Mock<IStatusManager> _statusManager;

        public SegmentTaskWorkerTests()
        {
            _segmentChangeFetcher = new Mock<ISegmentChangeFetcher>();
            _segmentCache = new Mock<ISegmentCache>();
            _statusManager = new Mock<IStatusManager>();
        }

        [TestMethod]
        public async Task NotifyWhenMultipleSegments()
        {
            // Arrange.
            var queue = new SplitQueue<SelfRefreshingSegment>();
            var worker = new SegmentTaskWorker(1, queue);

            _segmentCache
                .Setup(mock => mock.GetChangeNumber("segment-1"))
                .Returns(1);

            _segmentCache
                .Setup(mock => mock.GetChangeNumber("segment-2"))
                .Returns(2);

            _segmentCache
                .Setup(mock => mock.GetChangeNumber("segment-3"))
                .Returns(3);

            _segmentCache
                .Setup(mock => mock.GetChangeNumber("segment-4"))
                .Returns(4);

            // Act.
            await queue.EnqueueAsync(new SelfRefreshingSegment("segment-1", _segmentChangeFetcher.Object, _segmentCache.Object, _statusManager.Object));
            await queue.EnqueueAsync(new SelfRefreshingSegment("segment-2", _segmentChangeFetcher.Object, _segmentCache.Object, _statusManager.Object));
            await queue.EnqueueAsync(new SelfRefreshingSegment("segment-3", _segmentChangeFetcher.Object, _segmentCache.Object, _statusManager.Object));
            await queue.EnqueueAsync(new SelfRefreshingSegment("segment-4", _segmentChangeFetcher.Object, _segmentCache.Object, _statusManager.Object));
            await worker.Notify();

            // Assert.
            _segmentChangeFetcher.Verify(mock => mock.FetchAsync("segment-1", 1, It.IsAny<FetchOptions>()), Times.Once);
            _segmentChangeFetcher.Verify(mock => mock.FetchAsync("segment-2", 2, It.IsAny<FetchOptions>()), Times.Once);
            _segmentChangeFetcher.Verify(mock => mock.FetchAsync("segment-3", 3, It.IsAny<FetchOptions>()), Times.Once);
            _segmentChangeFetcher.Verify(mock => mock.FetchAsync("segment-4", 4, It.IsAny<FetchOptions>()), Times.Once);
        }

        [TestMethod]
        public async Task Notify()
        {
            // Arrange.
            var queue = new SplitQueue<SelfRefreshingSegment>();
            var worker = new SegmentTaskWorker(1, queue);

            _segmentCache
                .Setup(mock => mock.GetChangeNumber(It.IsAny<string>()))
                .Returns(100);

            // Act.
            for (int i = 0; i < 100; i++)
            {
                await queue.EnqueueAsync(new SelfRefreshingSegment($"segment-{i}", _segmentChangeFetcher.Object, _segmentCache.Object, _statusManager.Object));
            }
            await worker.Notify();

            // Assert.
            _segmentChangeFetcher.Verify(mock => mock.FetchAsync(It.IsAny<string>(), 100, It.IsAny<FetchOptions>()), Times.Exactly(100));
        }
    }
}
