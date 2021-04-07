using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Telemetry.Common
{
    [TestClass]
    public class TelemetrySyncTaskTests
    {
        private Mock<ITelemetryStorageConsumer> _telemetryStorage;
        private Mock<ITelemetryAPI> _telemetryAPI;
        private Mock<ISplitCache> _splitCache;
        private Mock<ISegmentCache> _segmentCache;
        private Mock<CancellationTokenSource> _cancellationTokenSource;
        private Mock<ISplitLogger> _log;

        private ITelemetrySyncTask _telemetrySyncTask;

        [TestInitialize]
        public void Initialization()
        {
            _telemetryStorage = new Mock<ITelemetryStorageConsumer>();
            _telemetryAPI = new Mock<ITelemetryAPI>();
            _splitCache = new Mock<ISplitCache>();
            _segmentCache = new Mock<ISegmentCache>();
            _cancellationTokenSource = new Mock<CancellationTokenSource>();
            _log = new Mock<ISplitLogger>();            
        }

        [TestMethod]
        public void StartShouldPostStats()
        {
            // Arrange.
            _telemetryStorage.Setup(mock => mock.PopAuthRejections()).Returns(2);
            _telemetryStorage.Setup(mock => mock.GetEventsStats(EventsEnum.EventsDropped)).Returns(3);
            _telemetryStorage.Setup(mock => mock.GetEventsStats(EventsEnum.EventsQueued)).Returns(4);
            _telemetryStorage.Setup(mock => mock.PopHttpErrors()).Returns(new HTTPErrors());
            _telemetryStorage.Setup(mock => mock.PopHttpLatencies()).Returns(new HTTPLatencies());
            _telemetryStorage.Setup(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped)).Returns(5);
            _telemetryStorage.Setup(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped)).Returns(6);
            _telemetryStorage.Setup(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued)).Returns(7);
            _telemetryStorage.Setup(mock => mock.GetLastSynchronizations()).Returns(new LastSynchronization());
            _telemetryStorage.Setup(mock => mock.PopExceptions()).Returns(new MethodExceptions());
            _telemetryStorage.Setup(mock => mock.PopLatencies()).Returns(new MethodLatencies());
            _telemetryStorage.Setup(mock => mock.GetSessionLength()).Returns(8);
            _telemetryStorage.Setup(mock => mock.PopStreamingEvents()).Returns(new List<StreamingEvent>());
            _telemetryStorage.Setup(mock => mock.PopTags()).Returns(new List<string>());
            _telemetryStorage.Setup(mock => mock.PopTokenRefreshes()).Returns(9);
            _splitCache.Setup(mock => mock.SplitsCount()).Returns(50);
            _segmentCache.Setup(mock => mock.SegmentsCount()).Returns(10);
            _segmentCache.Setup(mock => mock.SegmentKeysCount()).Returns(33);

            _telemetrySyncTask = new TelemetrySyncTask(_telemetryStorage.Object, _telemetryAPI.Object, _splitCache.Object, _segmentCache.Object, refreshRate: 2, log: _log.Object);

            // Act.
            _telemetrySyncTask.Start();
            Thread.Sleep(2000);

            // Assert.
            _telemetryAPI.Verify(mock=> mock.RecordStats(It.IsAny<Stats>()), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopAuthRejections(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetEventsStats(EventsEnum.EventsDropped), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetEventsStats(EventsEnum.EventsQueued), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopHttpErrors(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopHttpLatencies(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetLastSynchronizations(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopExceptions(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopLatencies(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetSessionLength(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopStreamingEvents(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopTags(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopTokenRefreshes(), Times.AtLeastOnce);
            _splitCache.Verify(mock => mock.SplitsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentKeysCount(), Times.AtLeastOnce);
        }

        [TestMethod]
        public void StopShouldPostStats()
        {
            // Arrange.
            _telemetryStorage.Setup(mock => mock.PopAuthRejections()).Returns(2);
            _telemetryStorage.Setup(mock => mock.GetEventsStats(EventsEnum.EventsDropped)).Returns(3);
            _telemetryStorage.Setup(mock => mock.GetEventsStats(EventsEnum.EventsQueued)).Returns(4);
            _telemetryStorage.Setup(mock => mock.PopHttpErrors()).Returns(new HTTPErrors());
            _telemetryStorage.Setup(mock => mock.PopHttpLatencies()).Returns(new HTTPLatencies());
            _telemetryStorage.Setup(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped)).Returns(5);
            _telemetryStorage.Setup(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped)).Returns(6);
            _telemetryStorage.Setup(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued)).Returns(7);
            _telemetryStorage.Setup(mock => mock.GetLastSynchronizations()).Returns(new LastSynchronization());
            _telemetryStorage.Setup(mock => mock.PopExceptions()).Returns(new MethodExceptions());
            _telemetryStorage.Setup(mock => mock.PopLatencies()).Returns(new MethodLatencies());
            _telemetryStorage.Setup(mock => mock.GetSessionLength()).Returns(8);
            _telemetryStorage.Setup(mock => mock.PopStreamingEvents()).Returns(new List<StreamingEvent>());
            _telemetryStorage.Setup(mock => mock.PopTags()).Returns(new List<string>());
            _telemetryStorage.Setup(mock => mock.PopTokenRefreshes()).Returns(9);
            _splitCache.Setup(mock => mock.SplitsCount()).Returns(50);
            _segmentCache.Setup(mock => mock.SegmentsCount()).Returns(10);
            _segmentCache.Setup(mock => mock.SegmentKeysCount()).Returns(33);

            _telemetrySyncTask = new TelemetrySyncTask(_telemetryStorage.Object, _telemetryAPI.Object, _splitCache.Object, _segmentCache.Object, refreshRate: 2, log: _log.Object);

            // Act.
            _telemetrySyncTask.Stop();
            Thread.Sleep(2000);

            // Assert.
            _telemetryAPI.Verify(mock => mock.RecordStats(It.IsAny<Stats>()), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopAuthRejections(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetEventsStats(EventsEnum.EventsDropped), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetEventsStats(EventsEnum.EventsQueued), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopHttpErrors(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopHttpLatencies(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetLastSynchronizations(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopExceptions(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopLatencies(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.GetSessionLength(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopStreamingEvents(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopTags(), Times.AtLeastOnce);
            _telemetryStorage.Verify(mock => mock.PopTokenRefreshes(), Times.AtLeastOnce);
            _splitCache.Verify(mock => mock.SplitsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentKeysCount(), Times.AtLeastOnce);
        }
    }
}
