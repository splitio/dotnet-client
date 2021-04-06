using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;

namespace Splitio_Tests.Unit_Tests.Telemetry.Storages
{
    [TestClass]
    public class InMemoryTelemetryStorageTests
    {
        private ITelemetryStorage _telemetryStorage;

        [TestInitialize]
        public void Initialization()
        {
            _telemetryStorage = new InMemoryTelemetryStorage();
        }

        [TestMethod]
        public void MethodLatenciesPopFirstTime()
        {            
            // Act.
            var result = _telemetryStorage.PopLatencies();

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
            _telemetryStorage.RecordLatency(MethodEnum.Treatment, 2343);
            _telemetryStorage.RecordLatency(MethodEnum.Treatment, 2344);
            _telemetryStorage.RecordLatency(MethodEnum.Treatments, 2345);
            _telemetryStorage.RecordLatency(MethodEnum.TreatmentsWithConfig, 2346);
            _telemetryStorage.RecordLatency(MethodEnum.Track, 78787);
            _telemetryStorage.RecordLatency(MethodEnum.Track, 678678);

            // Act.
            var result = _telemetryStorage.PopLatencies();

            // Assert.
            Assert.AreEqual(2, result.Treatment.Count);
            Assert.AreEqual(0, result.TreatmentWithConfig.Count);
            Assert.AreEqual(1, result.Treatments.Count);
            Assert.AreEqual(1, result.TreatmenstWithConfig.Count);
            Assert.AreEqual(2, result.Track.Count);

            result = _telemetryStorage.PopLatencies();
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
            var result = _telemetryStorage.PopHttpLatencies();

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
            _telemetryStorage.RecordSyncLatency(ResourceEnum.EventSync, 2);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.EventSync, 4);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.ImpressionSync, 2);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.ImpressionSync, 2);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.SegmentSync, 2);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.SplitSync, 2);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.SplitSync, 3);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.SplitSync, 4);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.TelemetrySync, 2);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.TelemetrySync, 7);

            // Act.
            var result = _telemetryStorage.PopHttpLatencies();

            // Assert.
            Assert.AreEqual(2, result.Events.Count);
            Assert.AreEqual(2, result.Impressions.Count);
            Assert.AreEqual(1, result.Segments.Count);
            Assert.AreEqual(3, result.Splits.Count);
            Assert.AreEqual(2, result.Telemetry.Count);
            Assert.AreEqual(0, result.Token.Count);

            result = _telemetryStorage.PopHttpLatencies();
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
            var result = _telemetryStorage.PopExceptions();

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
            _telemetryStorage.RecordException(MethodEnum.Treatment);
            _telemetryStorage.RecordException(MethodEnum.Treatment);
            _telemetryStorage.RecordException(MethodEnum.Treatments);
            _telemetryStorage.RecordException(MethodEnum.TreatmentsWithConfig);
            _telemetryStorage.RecordException(MethodEnum.TreatmentWithConfig);

            // Act.
            var result = _telemetryStorage.PopExceptions();

            // Assert.
            Assert.AreEqual(2, result.Treatment);
            Assert.AreEqual(1, result.Treatments);
            Assert.AreEqual(1, result.TreatmentWithConfig);
            Assert.AreEqual(1, result.TreatmentsWithConfig);
            Assert.AreEqual(0, result.Track);

            result = _telemetryStorage.PopExceptions();
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
            var resultAuth = _telemetryStorage.PopAuthRejections();
            var resultToken = _telemetryStorage.PopTokenRefreshes();

            // Assert.
            Assert.AreEqual(0, resultAuth);
            Assert.AreEqual(0, resultToken);
        }

        [TestMethod]
        public void PushCountersRecordAndPop()
        {
            // Arrange.
            _telemetryStorage.RecordAuthRejections();
            _telemetryStorage.RecordAuthRejections();
            _telemetryStorage.RecordAuthRejections();
            _telemetryStorage.RecordTokenRefreshes();
            _telemetryStorage.RecordTokenRefreshes();

            // Act.
            var resultAuth = _telemetryStorage.PopAuthRejections();
            var resultToken = _telemetryStorage.PopTokenRefreshes();

            // Assert.
            Assert.AreEqual(3, resultAuth);
            Assert.AreEqual(2, resultToken);

            resultAuth = _telemetryStorage.PopAuthRejections();
            resultToken = _telemetryStorage.PopTokenRefreshes();
            Assert.AreEqual(0, resultAuth);
            Assert.AreEqual(0, resultToken);
        }

        [TestMethod]
        public void FactoryCountersGetFirstTime()
        {
            // Act.
            var resultBur = _telemetryStorage.GetBURTimeouts();
            var resultNon = _telemetryStorage.GetNonReadyUsages();

            // Assert.
            Assert.AreEqual(0, resultBur);
            Assert.AreEqual(0, resultNon);
        }

        [TestMethod]
        public void FactoryCountersRecordAndGet()
        {
            // Arrange.
            _telemetryStorage.RecordBURTimeout();
            _telemetryStorage.RecordBURTimeout();
            _telemetryStorage.RecordBURTimeout();
            _telemetryStorage.RecordNonReadyUsages();
            _telemetryStorage.RecordNonReadyUsages();

            // Act.
            var resultBur = _telemetryStorage.GetBURTimeouts();
            var resultNon = _telemetryStorage.GetNonReadyUsages();

            // Assert.
            Assert.AreEqual(3, resultBur);
            Assert.AreEqual(2, resultNon);

            _telemetryStorage.RecordBURTimeout();
            _telemetryStorage.RecordNonReadyUsages();

            resultBur = _telemetryStorage.GetBURTimeouts();
            resultNon = _telemetryStorage.GetNonReadyUsages();

            Assert.AreEqual(4, resultBur);
            Assert.AreEqual(3, resultNon);
        }

        [TestMethod]
        public void ImpressionsDataRecordsGetFirstTime()
        {
            // Act.
            var resultDedupe = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped);
            var resultDropped = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped);
            var resultQueued = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued);

            // Assert.
            Assert.AreEqual(0, resultDedupe);
            Assert.AreEqual(0, resultDropped);
            Assert.AreEqual(0, resultQueued);
        }

        [TestMethod]
        public void ImpressionsDataRecordsRecordAndGet()
        {
            // Arrange.
            _telemetryStorage.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 2);
            _telemetryStorage.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 5);
            _telemetryStorage.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 2);
            _telemetryStorage.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 3);
            _telemetryStorage.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 2);

            // Act.
            var resultDedupe = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped);
            var resultDropped = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped);
            var resultQueued = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued);

            // Assert.
            Assert.AreEqual(7, resultDedupe);
            Assert.AreEqual(5, resultDropped);
            Assert.AreEqual(2, resultQueued);

            _telemetryStorage.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 2);
            _telemetryStorage.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 2);;
            _telemetryStorage.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 2);

            resultDedupe = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped);
            resultDropped = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped);
            resultQueued = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued);

            Assert.AreEqual(9, resultDedupe);
            Assert.AreEqual(7, resultDropped);
            Assert.AreEqual(4, resultQueued);
        }

        [TestMethod]
        public void EventsDataRecordsGetFirstTime()
        {
            // Act.
            var resultDropped = _telemetryStorage.GetEventsStats(EventsEnum.EventsDropped);
            var resultQueued = _telemetryStorage.GetEventsStats(EventsEnum.EventsQueued);

            // Assert.
            Assert.AreEqual(0, resultDropped);
            Assert.AreEqual(0, resultQueued);
        }

        [TestMethod]
        public void EventsDataRecordsRecordAndGet()
        {
            // Arrange.
            _telemetryStorage.RecordEventsStats(EventsEnum.EventsDropped, 2);
            _telemetryStorage.RecordEventsStats(EventsEnum.EventsDropped, 2);
            _telemetryStorage.RecordEventsStats(EventsEnum.EventsQueued, 2);

            // Act.
            var resultDropped = _telemetryStorage.GetEventsStats(EventsEnum.EventsDropped);
            var resultQueued = _telemetryStorage.GetEventsStats(EventsEnum.EventsQueued);

            // Assert.
            Assert.AreEqual(4, resultDropped);
            Assert.AreEqual(2, resultQueued);

            _telemetryStorage.RecordEventsStats(EventsEnum.EventsDropped, 2);
            _telemetryStorage.RecordEventsStats(EventsEnum.EventsQueued, 2);

            resultDropped = _telemetryStorage.GetEventsStats(EventsEnum.EventsDropped);
            resultQueued = _telemetryStorage.GetEventsStats(EventsEnum.EventsQueued);

            Assert.AreEqual(6, resultDropped);
            Assert.AreEqual(4, resultQueued);
        }

        [TestMethod]
        public void LastSynchronizationRecordsGetFirstTime()
        {
            // Act.
            var result = _telemetryStorage.GetLastSynchronizations();

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
            _telemetryStorage.RecordSuccessfulSync(ResourceEnum.EventSync, 123);
            _telemetryStorage.RecordSuccessfulSync(ResourceEnum.ImpressionSync, 2222);
            _telemetryStorage.RecordSuccessfulSync(ResourceEnum.SegmentSync, 3333);
            _telemetryStorage.RecordSuccessfulSync(ResourceEnum.SplitSync, 44444);
            _telemetryStorage.RecordSuccessfulSync(ResourceEnum.TelemetrySync, 66666);

            // Act.
            var result = _telemetryStorage.GetLastSynchronizations();

            // Assert.
            Assert.AreEqual(123, result.Events);
            Assert.AreEqual(2222, result.Impressions);
            Assert.AreEqual(3333, result.Segments);
            Assert.AreEqual(44444, result.Splits);
            Assert.AreEqual(66666, result.Telemetry);
            Assert.AreEqual(0, result.Token);

            _telemetryStorage.RecordSuccessfulSync(ResourceEnum.SplitSync, 8888888);
            _telemetryStorage.RecordSuccessfulSync(ResourceEnum.TelemetrySync, 99999);

            result = _telemetryStorage.GetLastSynchronizations();
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
            var result = _telemetryStorage.GetSessionLength();

            // Assert. 
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void SdkRecordsRecordAndGetFirsTime()
        {
            // Arrenge.
            _telemetryStorage.RecordSessionLength(3333);

            // Act.
            var result = _telemetryStorage.GetSessionLength();

            // Assert. 
            Assert.AreEqual(3333, result);

            _telemetryStorage.RecordSessionLength(565656);
            result = _telemetryStorage.GetSessionLength();
            Assert.AreEqual(565656, result);
        }
    }
}
