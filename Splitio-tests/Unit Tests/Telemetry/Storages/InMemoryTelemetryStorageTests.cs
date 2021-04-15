using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;

namespace Splitio_Tests.Unit_Tests.Telemetry.Storages
{
    [TestClass]
    public class InMemoryTelemetryStorageTests
    {
        private ITelemetryEvaluationProducer _telemetryEvaluationProducer;
        private ITelemetryInitProducer _telemetryInitProducer;
        private ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private ITelemetryEvaluationConsumer _telemetryEvaluationConsumer;
        private ITelemetryInitConsumer _telemetryInitConsumer;
        private ITelemetryRuntimeConsumer _telemetryRuntimeConsumer;

        [TestInitialize]
        public void Initialization()
        {
            var storage = new InMemoryTelemetryStorage();

            _telemetryEvaluationProducer = new TelemetryEvaluationProducer(storage);
            _telemetryInitProducer = new TelemetryInitProducer(storage);
            _telemetryRuntimeProducer = new TelemetryRuntimeProducer(storage);
            _telemetryEvaluationConsumer = new TelemetryEvaluationConsumer(storage);
            _telemetryInitConsumer = new TelemetryInitConsumer(storage);
            _telemetryRuntimeConsumer = new TelemetryRuntimeConsumer(storage);
        }

        [TestMethod]
        public void MethodLatenciesPopFirstTime()
        {            
            // Act.
            var result = _telemetryEvaluationConsumer.PopLatencies();

            // Assert.
            Assert.AreEqual(0, result.Treatment.Count);
            Assert.AreEqual(0, result.TreatmentWithConfig.Count);
            Assert.AreEqual(0, result.Treatments.Count);
            Assert.AreEqual(0, result.TreatmenstWithConfig.Count);
            Assert.AreEqual(0, result.Track.Count);
        }

        [TestMethod]
        public void MethodLatenciesRecordAndPop()
        {
            // Arrange.
            _telemetryEvaluationProducer.RecordLatency(MethodEnum.Treatment, 2343);
            _telemetryEvaluationProducer.RecordLatency(MethodEnum.Treatment, 2344);
            _telemetryEvaluationProducer.RecordLatency(MethodEnum.Treatments, 2345);
            _telemetryEvaluationProducer.RecordLatency(MethodEnum.TreatmentsWithConfig, 2346);
            _telemetryEvaluationProducer.RecordLatency(MethodEnum.Track, 78787);
            _telemetryEvaluationProducer.RecordLatency(MethodEnum.Track, 678678);

            // Act.
            var result = _telemetryEvaluationConsumer.PopLatencies();

            // Assert.
            Assert.AreEqual(2, result.Treatment.Count);
            Assert.AreEqual(0, result.TreatmentWithConfig.Count);
            Assert.AreEqual(1, result.Treatments.Count);
            Assert.AreEqual(1, result.TreatmenstWithConfig.Count);
            Assert.AreEqual(2, result.Track.Count);

            result = _telemetryEvaluationConsumer.PopLatencies();
            Assert.AreEqual(0, result.Treatment.Count);
            Assert.AreEqual(0, result.TreatmentWithConfig.Count);
            Assert.AreEqual(0, result.Treatments.Count);
            Assert.AreEqual(0, result.TreatmenstWithConfig.Count);
            Assert.AreEqual(0, result.Track.Count);
        }

        [TestMethod]
        public void HttpLatenciesPopFirstTime()
        {
            // Act.
            var result = _telemetryRuntimeConsumer.PopHttpLatencies();

            // Assert.            
            Assert.AreEqual(0, result.Events.Count);
            Assert.AreEqual(0, result.Impressions.Count);
            Assert.AreEqual(0, result.Segments.Count);
            Assert.AreEqual(0, result.Splits.Count);
            Assert.AreEqual(0, result.Telemetry.Count);
            Assert.AreEqual(0, result.Token.Count);
        }

        [TestMethod]
        public void HttpLatenciesRecordAndPop()
        {
            // Arrange.
            _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.EventSync, 2);
            _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.EventSync, 4);
            _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.ImpressionSync, 2);
            _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.ImpressionSync, 2);
            _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.SegmentSync, 2);
            _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.SplitSync, 2);
            _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.SplitSync, 3);
            _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.SplitSync, 4);
            _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.TelemetrySync, 2);
            _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.TelemetrySync, 7);

            // Act.
            var result = _telemetryRuntimeConsumer.PopHttpLatencies();

            // Assert.
            Assert.AreEqual(2, result.Events.Count);
            Assert.AreEqual(2, result.Impressions.Count);
            Assert.AreEqual(1, result.Segments.Count);
            Assert.AreEqual(3, result.Splits.Count);
            Assert.AreEqual(2, result.Telemetry.Count);
            Assert.AreEqual(0, result.Token.Count);

            result = _telemetryRuntimeConsumer.PopHttpLatencies();
            Assert.AreEqual(0, result.Events.Count);
            Assert.AreEqual(0, result.Impressions.Count);
            Assert.AreEqual(0, result.Segments.Count);
            Assert.AreEqual(0, result.Splits.Count);
            Assert.AreEqual(0, result.Telemetry.Count);
            Assert.AreEqual(0, result.Token.Count);
        }

        [TestMethod]
        public void ExceptionsCountersPopFirstTime()
        {
            // Act.
            var result = _telemetryEvaluationConsumer.PopExceptions();

            // Assert.
            Assert.AreEqual(0, result.Treatment);
            Assert.AreEqual(0, result.Treatments);
            Assert.AreEqual(0, result.TreatmentWithConfig);
            Assert.AreEqual(0, result.TreatmentsWithConfig);
            Assert.AreEqual(0, result.Track);
        }

        [TestMethod]
        public void ExceptionsCountersRecordAndPop()
        {
            // Arrange.
            _telemetryEvaluationProducer.RecordException(MethodEnum.Treatment);
            _telemetryEvaluationProducer.RecordException(MethodEnum.Treatment);
            _telemetryEvaluationProducer.RecordException(MethodEnum.Treatments);
            _telemetryEvaluationProducer.RecordException(MethodEnum.TreatmentsWithConfig);
            _telemetryEvaluationProducer.RecordException(MethodEnum.TreatmentWithConfig);

            // Act.
            var result = _telemetryEvaluationConsumer.PopExceptions();

            // Assert.
            Assert.AreEqual(2, result.Treatment);
            Assert.AreEqual(1, result.Treatments);
            Assert.AreEqual(1, result.TreatmentWithConfig);
            Assert.AreEqual(1, result.TreatmentsWithConfig);
            Assert.AreEqual(0, result.Track);

            result = _telemetryEvaluationConsumer.PopExceptions();
            Assert.AreEqual(0, result.Treatment);
            Assert.AreEqual(0, result.Treatments);
            Assert.AreEqual(0, result.TreatmentWithConfig);
            Assert.AreEqual(0, result.TreatmentsWithConfig);
            Assert.AreEqual(0, result.Track);
        }

        [TestMethod]
        public void PushCountersPopFirstTime()
        {
            // Act.
            var resultAuth = _telemetryRuntimeConsumer.PopAuthRejections();
            var resultToken = _telemetryRuntimeConsumer.PopTokenRefreshes();

            // Assert.
            Assert.AreEqual(0, resultAuth);
            Assert.AreEqual(0, resultToken);
        }

        [TestMethod]
        public void PushCountersRecordAndPop()
        {
            // Arrange.
            _telemetryRuntimeProducer.RecordAuthRejections();
            _telemetryRuntimeProducer.RecordAuthRejections();
            _telemetryRuntimeProducer.RecordAuthRejections();
            _telemetryRuntimeProducer.RecordTokenRefreshes();
            _telemetryRuntimeProducer.RecordTokenRefreshes();

            // Act.
            var resultAuth = _telemetryRuntimeConsumer.PopAuthRejections();
            var resultToken = _telemetryRuntimeConsumer.PopTokenRefreshes();

            // Assert.
            Assert.AreEqual(3, resultAuth);
            Assert.AreEqual(2, resultToken);

            resultAuth = _telemetryRuntimeConsumer.PopAuthRejections();
            resultToken = _telemetryRuntimeConsumer.PopTokenRefreshes();
            Assert.AreEqual(0, resultAuth);
            Assert.AreEqual(0, resultToken);
        }

        [TestMethod]
        public void FactoryCountersGetFirstTime()
        {
            // Act.
            var resultBur = _telemetryInitConsumer.GetBURTimeouts();
            var resultNon = _telemetryInitConsumer.GetNonReadyUsages();

            // Assert.
            Assert.AreEqual(0, resultBur);
            Assert.AreEqual(0, resultNon);
        }

        [TestMethod]
        public void FactoryCountersRecordAndGet()
        {
            // Arrange.
            _telemetryInitProducer.RecordBURTimeout();
            _telemetryInitProducer.RecordBURTimeout();
            _telemetryInitProducer.RecordBURTimeout();
            _telemetryInitProducer.RecordNonReadyUsages();
            _telemetryInitProducer.RecordNonReadyUsages();

            // Act.
            var resultBur = _telemetryInitConsumer.GetBURTimeouts();
            var resultNon = _telemetryInitConsumer.GetNonReadyUsages();

            // Assert.
            Assert.AreEqual(3, resultBur);
            Assert.AreEqual(2, resultNon);

            _telemetryInitProducer.RecordBURTimeout();
            _telemetryInitProducer.RecordNonReadyUsages();

            resultBur = _telemetryInitConsumer.GetBURTimeouts();
            resultNon = _telemetryInitConsumer.GetNonReadyUsages();

            Assert.AreEqual(4, resultBur);
            Assert.AreEqual(3, resultNon);
        }

        [TestMethod]
        public void ImpressionsDataRecordsGetFirstTime()
        {
            // Act.
            var resultDedupe = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped);
            var resultDropped = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped);
            var resultQueued = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued);

            // Assert.
            Assert.AreEqual(0, resultDedupe);
            Assert.AreEqual(0, resultDropped);
            Assert.AreEqual(0, resultQueued);
        }

        [TestMethod]
        public void ImpressionsDataRecordsRecordAndGet()
        {
            // Arrange.
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 2);
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 5);
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 2);
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 3);
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 2);

            // Act.
            var resultDedupe = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped);
            var resultDropped = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped);
            var resultQueued = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued);

            // Assert.
            Assert.AreEqual(7, resultDedupe);
            Assert.AreEqual(5, resultDropped);
            Assert.AreEqual(2, resultQueued);

            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 2);
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 2);;
            _telemetryRuntimeProducer.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 2);

            resultDedupe = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped);
            resultDropped = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped);
            resultQueued = _telemetryRuntimeConsumer.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued);

            Assert.AreEqual(9, resultDedupe);
            Assert.AreEqual(7, resultDropped);
            Assert.AreEqual(4, resultQueued);
        }

        [TestMethod]
        public void EventsDataRecordsGetFirstTime()
        {
            // Act.
            var resultDropped = _telemetryRuntimeConsumer.GetEventsStats(EventsEnum.EventsDropped);
            var resultQueued = _telemetryRuntimeConsumer.GetEventsStats(EventsEnum.EventsQueued);

            // Assert.
            Assert.AreEqual(0, resultDropped);
            Assert.AreEqual(0, resultQueued);
        }

        [TestMethod]
        public void EventsDataRecordsRecordAndGet()
        {
            // Arrange.
            _telemetryRuntimeProducer.RecordEventsStats(EventsEnum.EventsDropped, 2);
            _telemetryRuntimeProducer.RecordEventsStats(EventsEnum.EventsDropped, 2);
            _telemetryRuntimeProducer.RecordEventsStats(EventsEnum.EventsQueued, 2);

            // Act.
            var resultDropped = _telemetryRuntimeConsumer.GetEventsStats(EventsEnum.EventsDropped);
            var resultQueued = _telemetryRuntimeConsumer.GetEventsStats(EventsEnum.EventsQueued);

            // Assert.
            Assert.AreEqual(4, resultDropped);
            Assert.AreEqual(2, resultQueued);

            _telemetryRuntimeProducer.RecordEventsStats(EventsEnum.EventsDropped, 2);
            _telemetryRuntimeProducer.RecordEventsStats(EventsEnum.EventsQueued, 2);

            resultDropped = _telemetryRuntimeConsumer.GetEventsStats(EventsEnum.EventsDropped);
            resultQueued = _telemetryRuntimeConsumer.GetEventsStats(EventsEnum.EventsQueued);

            Assert.AreEqual(6, resultDropped);
            Assert.AreEqual(4, resultQueued);
        }

        [TestMethod]
        public void LastSynchronizationRecordsGetFirstTime()
        {
            // Act.
            var result = _telemetryRuntimeConsumer.GetLastSynchronizations();

            // Assert.
            Assert.AreEqual(0, result.Events);
            Assert.AreEqual(0, result.Impressions);
            Assert.AreEqual(0, result.Segments);
            Assert.AreEqual(0, result.Splits);
            Assert.AreEqual(0, result.Telemetry);
            Assert.AreEqual(0, result.Token);
        }

        [TestMethod]
        public void LastSynchronizationRecordsRecordAndGet()
        {
            // Arrange.
            _telemetryRuntimeProducer.RecordSuccessfulSync(ResourceEnum.EventSync, 123);
            _telemetryRuntimeProducer.RecordSuccessfulSync(ResourceEnum.ImpressionSync, 2222);
            _telemetryRuntimeProducer.RecordSuccessfulSync(ResourceEnum.SegmentSync, 3333);
            _telemetryRuntimeProducer.RecordSuccessfulSync(ResourceEnum.SplitSync, 44444);
            _telemetryRuntimeProducer.RecordSuccessfulSync(ResourceEnum.TelemetrySync, 66666);

            // Act.
            var result = _telemetryRuntimeConsumer.GetLastSynchronizations();

            // Assert.
            Assert.AreEqual(123, result.Events);
            Assert.AreEqual(2222, result.Impressions);
            Assert.AreEqual(3333, result.Segments);
            Assert.AreEqual(44444, result.Splits);
            Assert.AreEqual(66666, result.Telemetry);
            Assert.AreEqual(0, result.Token);

            _telemetryRuntimeProducer.RecordSuccessfulSync(ResourceEnum.SplitSync, 8888888);
            _telemetryRuntimeProducer.RecordSuccessfulSync(ResourceEnum.TelemetrySync, 99999);

            result = _telemetryRuntimeConsumer.GetLastSynchronizations();
            Assert.AreEqual(123, result.Events);
            Assert.AreEqual(2222, result.Impressions);
            Assert.AreEqual(3333, result.Segments);
            Assert.AreEqual(8888888, result.Splits);
            Assert.AreEqual(99999, result.Telemetry);
            Assert.AreEqual(0, result.Token);
        }

        [TestMethod]
        public void SdkRecordsGetFirsTime()
        {
            // Act.
            var result = _telemetryRuntimeConsumer.GetSessionLength();

            // Assert. 
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void SdkRecordsRecordAndGetFirsTime()
        {
            // Arrenge.
            _telemetryRuntimeProducer.RecordSessionLength(3333);

            // Act.
            var result = _telemetryRuntimeConsumer.GetSessionLength();

            // Assert. 
            Assert.AreEqual(3333, result);

            _telemetryRuntimeProducer.RecordSessionLength(565656);
            result = _telemetryRuntimeConsumer.GetSessionLength();
            Assert.AreEqual(565656, result);
        }
    }
}
