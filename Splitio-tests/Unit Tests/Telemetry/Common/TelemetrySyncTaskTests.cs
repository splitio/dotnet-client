﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
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
        private Mock<ITelemetryInitConsumer> _telemetryInitConsumer;
        private Mock<ITelemetryRuntimeConsumer> _telemetryRuntimeConsumer;
        private Mock<ITelemetryEvaluationConsumer> _telemetryEvaluationConsumer;
        private Mock<ITelemetryAPI> _telemetryAPI;
        private Mock<ISplitCache> _splitCache;
        private Mock<ISegmentCache> _segmentCache;
        private Mock<CancellationTokenSource> _cancellationTokenSource;
        private Mock<ISplitLogger> _log;
        private Mock<IReadinessGatesCache> _gates;
        private Mock<IFactoryInstantiationsService> _factoryInstantiationsService;

        private ITelemetrySyncTask _telemetrySyncTask;

        [TestInitialize]
        public void Initialization()
        {
            _telemetryInitConsumer = new Mock<ITelemetryInitConsumer>();
            _telemetryRuntimeConsumer = new Mock<ITelemetryRuntimeConsumer>();
            _telemetryEvaluationConsumer = new Mock<ITelemetryEvaluationConsumer>();
            _telemetryAPI = new Mock<ITelemetryAPI>();
            _splitCache = new Mock<ISplitCache>();
            _segmentCache = new Mock<ISegmentCache>();
            _cancellationTokenSource = new Mock<CancellationTokenSource>();
            _log = new Mock<ISplitLogger>();
            _gates = new Mock<IReadinessGatesCache>();
            _factoryInstantiationsService = new Mock<IFactoryInstantiationsService>();
        }

        [TestMethod]
        public void StartShouldPostConfigAndStats()
        {
            // Arrange.
            MockRecordStats();
            var config = MockConfigInit();

            _telemetrySyncTask = new TelemetrySyncTask(_telemetryInitConsumer.Object, _telemetryRuntimeConsumer.Object, _telemetryEvaluationConsumer.Object, _telemetryAPI.Object, _splitCache.Object, _segmentCache.Object, _gates.Object, config, _factoryInstantiationsService.Object, log: _log.Object);

            // Act.
            _telemetrySyncTask.Start();
            Thread.Sleep(2000);

            // Assert.
            _telemetryAPI.Verify(mock => mock.RecordConfigInit(It.IsAny<Config>()), Times.Once);
            _telemetryAPI.Verify(mock => mock.RecordStats(It.IsAny<Stats>()), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopAuthRejections(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetEventsStats(EventsEnum.EventsDropped), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetEventsStats(EventsEnum.EventsQueued), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopHttpErrors(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopHttpLatencies(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetLastSynchronizations(), Times.AtLeastOnce);
            _telemetryEvaluationConsumer.Verify(mock => mock.PopExceptions(), Times.AtLeastOnce);
            _telemetryEvaluationConsumer.Verify(mock => mock.PopLatencies(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetSessionLength(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopStreamingEvents(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopTags(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopTokenRefreshes(), Times.AtLeastOnce);
            _splitCache.Verify(mock => mock.SplitsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentKeysCount(), Times.AtLeastOnce);
        }

        [TestMethod]
        public void StopShouldPostStats()
        {
            // Arrange.
            MockRecordStats();

            _telemetrySyncTask = new TelemetrySyncTask(_telemetryInitConsumer.Object, _telemetryRuntimeConsumer.Object, _telemetryEvaluationConsumer.Object, _telemetryAPI.Object, _splitCache.Object, _segmentCache.Object, _gates.Object, new SelfRefreshingConfig(), _factoryInstantiationsService.Object, log: _log.Object);

            // Act.
            _telemetrySyncTask.Stop();
            Thread.Sleep(2000);

            // Assert.
            _telemetryAPI.Verify(mock => mock.RecordStats(It.IsAny<Stats>()), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopAuthRejections(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetEventsStats(EventsEnum.EventsDropped), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetEventsStats(EventsEnum.EventsQueued), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopHttpErrors(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopHttpLatencies(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetLastSynchronizations(), Times.AtLeastOnce);
            _telemetryEvaluationConsumer.Verify(mock => mock.PopExceptions(), Times.AtLeastOnce);
            _telemetryEvaluationConsumer.Verify(mock => mock.PopLatencies(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.GetSessionLength(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopStreamingEvents(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopTags(), Times.AtLeastOnce);
            _telemetryRuntimeConsumer.Verify(mock => mock.PopTokenRefreshes(), Times.AtLeastOnce);
            _splitCache.Verify(mock => mock.SplitsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentsCount(), Times.AtLeastOnce);
            _segmentCache.Verify(mock => mock.SegmentKeysCount(), Times.AtLeastOnce);
        }

        private SelfRefreshingConfig MockConfigInit()
        {
            _telemetryInitConsumer.Setup(mock => mock.GetBURTimeouts()).Returns(2);
            _factoryInstantiationsService.Setup(mock => mock.GetActiveFactories()).Returns(5);
            _factoryInstantiationsService.Setup(mock => mock.GetRedundantActiveFactories()).Returns(8);
            _telemetryInitConsumer.Setup(mock => mock.GetNonReadyUsages()).Returns(10);

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
            _telemetryRuntimeConsumer.Setup(mock => mock.PopAuthRejections()).Returns(2);
            _telemetryRuntimeConsumer.Setup(mock => mock.GetEventsStats(EventsEnum.EventsDropped)).Returns(3);
            _telemetryRuntimeConsumer.Setup(mock => mock.GetEventsStats(EventsEnum.EventsQueued)).Returns(4);
            _telemetryRuntimeConsumer.Setup(mock => mock.PopHttpErrors()).Returns(new HTTPErrors());
            _telemetryRuntimeConsumer.Setup(mock => mock.PopHttpLatencies()).Returns(new HTTPLatencies());
            _telemetryRuntimeConsumer.Setup(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped)).Returns(5);
            _telemetryRuntimeConsumer.Setup(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped)).Returns(6);
            _telemetryRuntimeConsumer.Setup(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued)).Returns(7);
            _telemetryRuntimeConsumer.Setup(mock => mock.GetLastSynchronizations()).Returns(new LastSynchronization());
            _telemetryEvaluationConsumer.Setup(mock => mock.PopExceptions()).Returns(new MethodExceptions());
            _telemetryEvaluationConsumer.Setup(mock => mock.PopLatencies()).Returns(new MethodLatencies());
            _telemetryRuntimeConsumer.Setup(mock => mock.GetSessionLength()).Returns(8);
            _telemetryRuntimeConsumer.Setup(mock => mock.PopStreamingEvents()).Returns(new List<StreamingEvent>());
            _telemetryRuntimeConsumer.Setup(mock => mock.PopTags()).Returns(new List<string>());
            _telemetryRuntimeConsumer.Setup(mock => mock.PopTokenRefreshes()).Returns(9);
            _splitCache.Setup(mock => mock.SplitsCount()).Returns(50);
            _segmentCache.Setup(mock => mock.SegmentsCount()).Returns(10);
            _segmentCache.Setup(mock => mock.SegmentKeysCount()).Returns(33);
        }
    }
}