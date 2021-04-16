using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.SegmentFetcher.Interfaces;
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

            _synchronizer = new Synchronizer(_splitFetcher.Object, _segmentFetcher.Object, _impressionsLog.Object, _eventsLog.Object, _impressionsCountSender.Object, _wrapperAdapter.Object, _gates.Object, _telemetrySyncTask.Object, _log.Object);
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
            _synchronizer.SyncAll();

            // Assert.
            Thread.Sleep(500);
            _splitFetcher.Verify(mock => mock.FetchSplits(), Times.Once);            
            _segmentFetcher.Verify(mock => mock.FetchAll(), Times.Once);
            _gates.Verify(mock => mock.SdkInternalReady(), Times.Once);
        }

        [TestMethod]
        public void SynchronizeSegment_ShouldFetchSegmentByName()
        {
            // Arrange.
            var segmentName = "segment-test";

            // Act.
            _synchronizer.SynchronizeSegment(segmentName);

            // Assert.
            _segmentFetcher.Verify(mock => mock.Fetch(segmentName), Times.Once);
        }

        [TestMethod]
        public void SynchronizeSplits_ShouldFetchSplits()
        {
            // Act.
            _synchronizer.SynchronizeSplits();

            // Assert.
            _splitFetcher.Verify(mock => mock.FetchSplits(), Times.Once);
            _segmentFetcher.Verify(mock => mock.FetchSegmentsIfNotExists(It.IsAny<IList<string>>()), Times.Once);
        }
    }
}
