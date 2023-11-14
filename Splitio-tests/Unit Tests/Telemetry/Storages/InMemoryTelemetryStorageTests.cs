using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Linq;

namespace Splitio_Tests.Unit_Tests.Telemetry.Storages
{
    [TestClass]
    public class InMemoryTelemetryStorageTests
    {
        private InMemoryTelemetryStorage _telemetryStorage;

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
            Assert.AreEqual(0, result.TreatmentsWithConfig.Count);
            Assert.AreEqual(0, result.Track.Count);
        }

        [TestMethod]
        public void MethodLatenciesRecordAndPop()
        {
            // Arrange.
            _telemetryStorage.RecordLatency(MethodEnum.Treatment, 5);
            _telemetryStorage.RecordLatency(MethodEnum.Treatment, 2);
            _telemetryStorage.RecordLatency(MethodEnum.Treatments, 3);
            _telemetryStorage.RecordLatency(MethodEnum.TreatmentsWithConfig, 4);
            _telemetryStorage.RecordLatency(MethodEnum.Track, 8);
            _telemetryStorage.RecordLatency(MethodEnum.Track, 1);

            var treatmentExpected = new long[] { 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var treatmentsExpected = new long[] { 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var treatmentsWithConfigExpected = new long[] { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var trackExpected = new long[] { 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            // Act.
            var result = _telemetryStorage.PopLatencies();

            // Assert.
            CollectionAssert.AreEqual(treatmentExpected, result.Treatment.ToArray());
            CollectionAssert.AreEqual(treatmentsExpected, result.Treatments.ToArray());
            CollectionAssert.AreEqual(treatmentsWithConfigExpected, result.TreatmentsWithConfig.ToArray());
            CollectionAssert.AreEqual(trackExpected, result.Track.ToArray());
            Assert.AreEqual(0, result.TreatmentWithConfig.Count);

            result = _telemetryStorage.PopLatencies();
            Assert.AreEqual(0, result.Treatment.Count);
            Assert.AreEqual(0, result.TreatmentWithConfig.Count);
            Assert.AreEqual(0, result.Treatments.Count);
            Assert.AreEqual(0, result.TreatmentsWithConfig.Count);
            Assert.AreEqual(0, result.Track.Count);

            _telemetryStorage.RecordLatency(MethodEnum.Treatment, 1);
            _telemetryStorage.RecordLatency(MethodEnum.Treatment, 2);
            _telemetryStorage.RecordLatency(MethodEnum.Treatments, 3);
            _telemetryStorage.RecordLatency(MethodEnum.TreatmentsWithConfig, 4);
            _telemetryStorage.RecordLatency(MethodEnum.Track, 5);
            _telemetryStorage.RecordLatency(MethodEnum.Track, 6);

            treatmentExpected = new long[] { 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            treatmentsExpected = new long[] { 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            treatmentsWithConfigExpected = new long[] { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            trackExpected = new long[] { 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            result = _telemetryStorage.PopLatencies();

            // Assert.
            CollectionAssert.AreEqual(treatmentExpected, result.Treatment.ToArray());
            CollectionAssert.AreEqual(treatmentsExpected, result.Treatments.ToArray());
            CollectionAssert.AreEqual(treatmentsWithConfigExpected, result.TreatmentsWithConfig.ToArray());
            CollectionAssert.AreEqual(trackExpected, result.Track.ToArray());
            Assert.AreEqual(0, result.TreatmentWithConfig.Count);
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
            _telemetryStorage.RecordSyncLatency(ResourceEnum.EventSync, 2);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.EventSync, 4);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.ImpressionSync, 2);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.ImpressionSync, 2);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.SegmentSync, 3);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.SplitSync, 2);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.SplitSync, 3);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.SplitSync, 4);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.TelemetrySync, 2);
            _telemetryStorage.RecordSyncLatency(ResourceEnum.TelemetrySync, 7);

            var eventsExpected = new long[] { 0, 0, 2, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var impressionsExpected = new long[] { 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var segmentsExpected = new long[] { 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var splitsExpected = new long[] { 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var telemetryExpected = new long[] { 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            // Act.
            var result = _telemetryStorage.PopHttpLatencies();

            // Assert.
            CollectionAssert.AreEqual(eventsExpected, result.Events.ToArray());
            CollectionAssert.AreEqual(impressionsExpected, result.Impressions.ToArray());
            CollectionAssert.AreEqual(segmentsExpected, result.Segments.ToArray());
            CollectionAssert.AreEqual(splitsExpected, result.Splits.ToArray());
            CollectionAssert.AreEqual(telemetryExpected, result.Telemetry.ToArray());
            Assert.AreEqual(0, result.Token.Count);
            Assert.AreEqual(0, result.ImpressionCount.Count);

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
            // Arrange.
            _telemetryStorage.RecordSessionLength(3333);

            // Act.
            var result = _telemetryStorage.GetSessionLength();

            // Assert. 
            Assert.AreEqual(3333, result);

            _telemetryStorage.RecordSessionLength(565656);
            result = _telemetryStorage.GetSessionLength();
            Assert.AreEqual(565656, result);
        }

        [TestMethod]
        public void StreamingEventsRecordAndPop()
        {
            // Arrange.
            _telemetryStorage.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SSEConnectionEstablished, 0));
            _telemetryStorage.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.OccupancyPri, 2));
            _telemetryStorage.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.OccupancySec, 0));

            // Act.
            var result = _telemetryStorage.PopStreamingEvents();

            // Assert.
            Assert.AreEqual(3, result.Count);

            result = _telemetryStorage.PopStreamingEvents();
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TagsEventsRecordAndPop()
        {
            // Arrange.
            _telemetryStorage.AddTag("tag-1");
            _telemetryStorage.AddTag("tag-2");
            _telemetryStorage.AddTag("tag-3");
            _telemetryStorage.AddTag("tag-4");

            // Act.
            var result = _telemetryStorage.PopTags();

            // Assert.
            Assert.AreEqual(4, result.Count);

            result = _telemetryStorage.PopTags();
            Assert.AreEqual(0, result.Count);
        }
    }
}
