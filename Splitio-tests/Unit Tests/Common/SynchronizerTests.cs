using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Telemetry.Common;
using System.Collections.Generic;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class SynchronizerTests
    {
        private readonly Mock<ISplitFetcher> _splitFetcher;
        private readonly Mock<ISelfRefreshingSegmentFetcher> _segmentFetcher;
        private readonly Mock<IImpressionsLog> _impressionsLog;
        private readonly Mock<IEventsLog> _eventsLog;
        private readonly Mock<ISplitLogger> _log;
        private readonly Mock<IImpressionsCountSender> _impressionsCountSender;
        private readonly Mock<IWrapperAdapter> _wrapperAdapter;
        private readonly Mock<IReadinessGatesCache> _gates;
        private readonly Mock<ITelemetrySyncTask> _telemetrySyncTask;
        private readonly Mock<ISplitCache> _splitCache;
        private readonly Mock<ISegmentCache> _segmentCache;
        private readonly Mock<IBackOff> _backOff;
        private readonly ISynchronizer _synchronizer;

        public SynchronizerTests()
        {
            _splitFetcher = new Mock<ISplitFetcher>();
            _segmentFetcher = new Mock<ISelfRefreshingSegmentFetcher>();
            _impressionsLog = new Mock<IImpressionsLog>();
            _eventsLog = new Mock<IEventsLog>();
            _log = new Mock<ISplitLogger>();
            _impressionsCountSender = new Mock<IImpressionsCountSender>();
            _wrapperAdapter = new Mock<IWrapperAdapter>();
            _gates = new Mock<IReadinessGatesCache>();
            _telemetrySyncTask = new Mock<ITelemetrySyncTask>();
            _splitCache = new Mock<ISplitCache>();
            _backOff = new Mock<IBackOff>();
            _segmentCache = new Mock<ISegmentCache>();

            _synchronizer = new Synchronizer(_splitFetcher.Object, _segmentFetcher.Object, _impressionsLog.Object, _eventsLog.Object, _impressionsCountSender.Object, _wrapperAdapter.Object, _gates.Object, _telemetrySyncTask.Object, new TasksManager(_wrapperAdapter.Object), _splitCache.Object, _backOff.Object, 10, 5, _segmentCache.Object, _log.Object);
        }

        [TestMethod]
        public void StartPeriodicDataRecording_ShouldStartServices()
        {
            // Act.
            _synchronizer.StartPeriodicDataRecording();

            // Assert.
            _impressionsLog.Verify(mock => mock.Start(), Times.Once);
            _eventsLog.Verify(mock => mock.Start(), Times.Once);
            _impressionsCountSender.Verify(mock => mock.Start(), Times.Once);
            _telemetrySyncTask.Verify(mock => mock.Start(), Times.Once);
        }

        [TestMethod]
        public void StartPeriodicFetching_ShouldStartFetchings()
        {
            // Act.
            _synchronizer.StartPeriodicFetching();
            Thread.Sleep(1000);

            // Assert.
            _splitFetcher.Verify(mock => mock.Start(), Times.Once);
            _segmentFetcher.Verify(mock => mock.Start(), Times.Once);
        }

        [TestMethod]
        public void StopPeriodicDataRecording_ShouldStopServices()
        {
            // Act.
            _synchronizer.StopPeriodicDataRecording();

            // Assert.
            _impressionsLog.Verify(mock => mock.Stop(), Times.Once);
            _eventsLog.Verify(mock => mock.Stop(), Times.Once);
            _impressionsCountSender.Verify(mock => mock.Stop(), Times.Once);
            _telemetrySyncTask.Verify(mock => mock.Stop(), Times.Once);
        }

        [TestMethod]
        public void StopPeriodicFetching_ShouldStopFetchings()
        {
            // Act.
            _synchronizer.StopPeriodicFetching();

            // Assert.
            _splitFetcher.Verify(mock => mock.Stop(), Times.Once);
            _segmentFetcher.Verify(mock => mock.Stop(), Times.Once);
        }

        [TestMethod]
        public void SyncAll_ShouldStartFetchSplitsAndSegments()
        {
            // Act.
            _splitFetcher
                .Setup(mock => mock.FetchSplits(It.IsAny<FetchOptions>()))
                .ReturnsAsync(new FetchResult { Success = true });

            _synchronizer.SyncAll(new CancellationTokenSource());

            // Assert.
            Thread.Sleep(2000);
            _splitFetcher.Verify(mock => mock.FetchSplits(It.IsAny<FetchOptions>()), Times.Once);            
            _segmentFetcher.Verify(mock => mock.FetchAll(), Times.Once);
        }

        [TestMethod]
        public void SynchronizeSegment_ShouldFetchSegmentByName()
        {
            // Arrange.
            var segmentName = "segment-test";

            _segmentCache
                .SetupSequence(mock => mock.GetChangeNumber(segmentName))
                .Returns(-1)
                .Returns(2);

            // Act.
            _synchronizer.SynchronizeSegment(segmentName, 1);

            // Assert.
            _segmentFetcher.Verify(mock => mock.Fetch(segmentName, It.IsAny<FetchOptions>()), Times.Once);
        }

        [TestMethod]
        public void SynchronizeSegment_NoChangesFetched()
        {
            // Arrange.
            var segmentName = "segment-test";

            _segmentCache
                .Setup(mock => mock.GetChangeNumber(segmentName))
                .Returns(2);

            // Act.
            _synchronizer.SynchronizeSegment(segmentName, 100);

            // Assert.
            _segmentFetcher.Verify(mock => mock.Fetch(segmentName, It.IsAny<FetchOptions>()), Times.Exactly(20));
            _log.Verify(mock => mock.Debug($"No changes fetched for segment {segmentName} after 10 attempts with CDN bypassed."), Times.Once);
            _log.Verify(mock => mock.Debug(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void SynchronizeSegment_With5Attempts()
        {
            // Arrange.
            var segmentName = "segment-test";

            _segmentCache
                .SetupSequence(mock => mock.GetChangeNumber(segmentName))
                .Returns(-1)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(110);

            // Act.
            _synchronizer.SynchronizeSegment(segmentName, 100);

            // Assert.
            _segmentFetcher.Verify(mock => mock.Fetch(segmentName, It.IsAny<FetchOptions>()), Times.Exactly(5));
            _log.Verify(mock => mock.Debug($"Segment {segmentName} refresh completed in 5 attempts."), Times.Once);
            _log.Verify(mock => mock.Debug(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void SynchronizeSegment_WithCDNBypassed()
        {
            // Arrange.
            var segmentName = "segment-test";

            _segmentCache
                .SetupSequence(mock => mock.GetChangeNumber(segmentName))
                .Returns(-1)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(110);

            // Act.
            _synchronizer.SynchronizeSegment(segmentName, 100);

            // Assert.
            _segmentFetcher.Verify(mock => mock.Fetch(segmentName, It.IsAny<FetchOptions>()), Times.Exactly(17));
            _log.Verify(mock => mock.Debug($"Segment {segmentName} refresh completed bypassing the CDN in 7 attempts."), Times.Once);
            _log.Verify(mock => mock.Debug(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void SynchronizeSplits_ShouldFetchSplits()
        {
            // Arrange.
            _splitFetcher
                .Setup(mock => mock.FetchSplits(It.IsAny<FetchOptions>()))
                .ReturnsAsync(new FetchResult());

            _splitCache
                .SetupSequence(mock => mock.GetChangeNumber())
                .Returns(-1)
                .Returns(2);

            // Act.
            _synchronizer.SynchronizeSplits(1);

            // Assert.
            _splitFetcher.Verify(mock => mock.FetchSplits(It.IsAny<FetchOptions>()), Times.Once);
            _segmentFetcher.Verify(mock => mock.FetchSegmentsIfNotExists(It.IsAny<IList<string>>()), Times.Once);
        }

        [TestMethod]
        public void SynchronizeSplits_NoChangesFetched()
        {
            // Arrange.
            _splitCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(2);

            // Act.
            _synchronizer.SynchronizeSplits(100);

            // Assert.
            _splitFetcher.Verify(mock => mock.FetchSplits(It.IsAny<FetchOptions>()), Times.Exactly(20));
            _log.Verify(mock => mock.Debug($"No changes fetched after 10 attempts with CDN bypassed."), Times.Once);
            _log.Verify(mock => mock.Debug(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void SynchronizeSplits_With5Attempts()
        {
            // Arrange.
            _splitFetcher
                .Setup(mock => mock.FetchSplits(It.IsAny<FetchOptions>()))
                .ReturnsAsync(new FetchResult());

            _splitCache
                .SetupSequence(mock => mock.GetChangeNumber())
                .Returns(-1)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(110);

            // Act.
            _synchronizer.SynchronizeSplits(100);

            // Assert.
            _splitFetcher.Verify(mock => mock.FetchSplits(It.IsAny<FetchOptions>()), Times.Exactly(5));
            _log.Verify(mock => mock.Debug($"Refresh completed in 5 attempts."), Times.Once);
            _log.Verify(mock => mock.Debug(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void SynchronizeSplits_WithCDNBypassed()
        {
            // Arrange.
            _splitFetcher
                .Setup(mock => mock.FetchSplits(It.IsAny<FetchOptions>()))
                .ReturnsAsync(new FetchResult());

            _splitCache
                .SetupSequence(mock => mock.GetChangeNumber())
                .Returns(-1)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(2)
                .Returns(110);

            // Act.
            _synchronizer.SynchronizeSplits(100);

            // Assert.
            _splitFetcher.Verify(mock => mock.FetchSplits(It.IsAny<FetchOptions>()), Times.Exactly(17));
            _log.Verify(mock => mock.Debug($"Refresh completed bypassing the CDN in 7 attempts."), Times.Once);
            _log.Verify(mock => mock.Debug(It.IsAny<string>()), Times.Once);
        }
    }
}
