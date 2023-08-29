using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.Tasks;
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
        private Mock<IFactoryInstantiationsService> _factoryInstantiationsService;
        private Mock<IStatusManager> _statusManager;

        private ITelemetrySyncTask _telemetrySyncTask;

        [TestInitialize]
        public void Initialization()
        {
            _telemetryStorage = new Mock<ITelemetryStorageConsumer>();
            _telemetryAPI = new Mock<ITelemetryAPI>();
            _splitCache = new Mock<ISplitCache>();
            _segmentCache = new Mock<ISegmentCache>();
            _factoryInstantiationsService = new Mock<IFactoryInstantiationsService>();
            _statusManager = new Mock<IStatusManager>();
        }

        [TestMethod]
        public void StartShouldPostConfigAndStats()
        {
            // Arrange.
            var tasksManager = new TasksManager(_statusManager.Object);
            var statsTask = tasksManager.NewPeriodicTask(Splitio.Enums.Task.TelemetryStats, 500);
            var initTask = tasksManager.NewOnTimeTask(Splitio.Enums.Task.TelemetryInit);

            MockRecordStats();
            var config = MockConfigInit();

            _telemetrySyncTask = new TelemetrySyncTask(
                _telemetryStorage.Object,
                _telemetryAPI.Object,
                _splitCache.Object,
                _segmentCache.Object,
                config,
                _factoryInstantiationsService.Object,
                statsTask,
                initTask
            );

            // Act.
            _telemetrySyncTask.Start();
            _telemetrySyncTask.RecordConfigInit(1000);
            Thread.Sleep(2000);

            // Assert.
            _telemetryAPI.Verify(mock => mock.RecordConfigInitAsync(It.IsAny<Config>()), Times.Once);
            _telemetryAPI.Verify(mock => mock.RecordStatsAsync(It.IsAny<Stats>()), Times.AtLeastOnce);
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
            _telemetryStorage.Verify(mock => mock.PopUpdatesFromSSE(), Times.AtLeastOnce);
            _splitCache.Verify(mock => mock.SplitsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentKeysCount(), Times.AtLeastOnce);
        }

        [TestMethod]
        public void StopShouldPostStats()
        {
            // Arrange.
            MockRecordStats();
            var tasksManager = new TasksManager(_statusManager.Object);
            var statsTask = tasksManager.NewPeriodicTask(Splitio.Enums.Task.TelemetryStats, 500);
            var initTask = tasksManager.NewOnTimeTask(Splitio.Enums.Task.TelemetryInit);
            _telemetrySyncTask = new TelemetrySyncTask(_telemetryStorage.Object, _telemetryAPI.Object, _splitCache.Object, _segmentCache.Object, new SelfRefreshingConfig(), _factoryInstantiationsService.Object, statsTask, initTask);

            // Act.
            _telemetrySyncTask.Start();
            _telemetrySyncTask.Stop();
            Thread.Sleep(2000);

            // Assert.
            _telemetryAPI.Verify(mock => mock.RecordStatsAsync(It.IsAny<Stats>()), Times.AtLeastOnce);
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
            _telemetryStorage.Verify(mock => mock.PopUpdatesFromSSE(), Times.AtLeastOnce);
            _splitCache.Verify(mock => mock.SplitsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentKeysCount(), Times.AtLeastOnce);
        }

        private SelfRefreshingConfig MockConfigInit()
        {
            _telemetryStorage.Setup(mock => mock.GetBURTimeouts()).Returns(2);
            _factoryInstantiationsService.Setup(mock => mock.GetActiveFactories()).Returns(5);
            _factoryInstantiationsService.Setup(mock => mock.GetRedundantActiveFactories()).Returns(8);
            _telemetryStorage.Setup(mock => mock.GetNonReadyUsages()).Returns(10);

            return new SelfRefreshingConfig
            {
                EventLogSize = 2,
                EventLogRefreshRate = 60,
                TreatmentLogRefreshRate = 60,
                SegmentRefreshRate = 60,
                SplitsRefreshRate = 60,
                TelemetryRefreshRate = 60,
                ImpressionsMode = ImpressionsMode.Optimized,
                Mode = Splitio.Services.Client.Classes.Mode.Standalone,
                TreatmentLogSize = 4,
                SdkStartTime = CurrentTimeHelper.CurrentTimeMillis(),
                BaseUrl = "https://sdk.split.io",
                EventsBaseUrl = "https://events.split.io",
                AuthServiceURL = "https://auth.split.io",
                StreamingServiceURL = "https://streaming.split.io",
                TelemetryServiceURL = "https://telemetry.split.io"
            };
        }

        private void MockRecordStats()
        {
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
            _telemetryStorage.Setup(mock => mock.PopUpdatesFromSSE()).Returns(new UpdatesFromSSE { Splits = 11 });
            _splitCache.Setup(mock => mock.SplitsCount()).Returns(50);
            _segmentCache.Setup(mock => mock.SegmentsCount()).Returns(10);
            _segmentCache.Setup(mock => mock.SegmentKeysCount()).Returns(33);
        }
    }
}
