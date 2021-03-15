using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Services;
using Splitio.Telemetry.Services.Interfaces;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Telemetry.Services
{
    [TestClass]
    public class TelemetryFacadeTests
    {        
        private Mock<ITelemetryStorage> _telemetryStorage;
        private Mock<ISplitCache> _splitCache;
        private Mock<ISegmentCache> _segmentCache;

        private ITelemetryFacade _telemetryFacade;

        [TestInitialize]
        public void Initialization()
        {
            _telemetryStorage = new Mock<ITelemetryStorage>();
            _splitCache = new Mock<ISplitCache>();
            _segmentCache = new Mock<ISegmentCache>();

            _telemetryFacade = new TelemetryFacade(_telemetryStorage.Object, _splitCache.Object, _segmentCache.Object);
        }

        #region Producer Methods
        [TestMethod]
        public void RecordExceptionTest()
        {
            // Act.
            _telemetryFacade.RecordException(MethodEnum.Treatment);
            _telemetryFacade.RecordException(MethodEnum.TreatmentWithConfig);
            _telemetryFacade.RecordException(MethodEnum.Treatments);
            _telemetryFacade.RecordException(MethodEnum.Treatment);

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordException(MethodEnum.Treatment), Times.Exactly(2));
            _telemetryStorage.Verify(mock => mock.RecordException(MethodEnum.TreatmentWithConfig), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordException(MethodEnum.Treatments), Times.Once);
        }

        [TestMethod]
        public void RecordLatencyTest()
        {
            // Act.
            _telemetryFacade.RecordLatency(MethodEnum.Treatment, 342543);
            _telemetryFacade.RecordLatency(MethodEnum.Treatments, 222);
            _telemetryFacade.RecordLatency(MethodEnum.TreatmentWithConfig, 333);
            _telemetryFacade.RecordLatency(MethodEnum.Treatment, 111);

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordLatency(MethodEnum.Treatment, 22), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordLatency(MethodEnum.Treatments, 14), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordLatency(MethodEnum.TreatmentWithConfig, 15), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordLatency(MethodEnum.Treatment, 12), Times.Once);
        }

        [TestMethod]
        public void RecordImpressionsStatsTest()
        {
            // Act.
            _telemetryFacade.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 1);
            _telemetryFacade.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 2);
            _telemetryFacade.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 3);

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 1), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 2), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 3), Times.Once);
        }

        [TestMethod]
        public void RecordEventsStatsTest()
        {
            // Act.
            _telemetryFacade.RecordEventsStats(EventsEnum.EventsDropped, 2);
            _telemetryFacade.RecordEventsStats(EventsEnum.EventsQueued, 3);

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordEventsStats(EventsEnum.EventsDropped, 2), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordEventsStats(EventsEnum.EventsQueued, 3), Times.Once);
        }

        [TestMethod]
        public void RecordSuccessfulSyncTest()
        {
            // Act.
            _telemetryFacade.RecordSuccessfulSync(ResourceEnum.EventSync);
            _telemetryFacade.RecordSuccessfulSync(ResourceEnum.Impressionsync);
            _telemetryFacade.RecordSuccessfulSync(ResourceEnum.SegmentSync);
            _telemetryFacade.RecordSuccessfulSync(ResourceEnum.SplitSync);

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordSuccessfulSync(ResourceEnum.EventSync, It.IsAny<long>()), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordSuccessfulSync(ResourceEnum.Impressionsync, It.IsAny<long>()), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordSuccessfulSync(ResourceEnum.SegmentSync, It.IsAny<long>()), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordSuccessfulSync(ResourceEnum.SplitSync, It.IsAny<long>()), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordSuccessfulSync(ResourceEnum.TelemetrySync, It.IsAny<long>()), Times.Never);
            _telemetryStorage.Verify(mock => mock.RecordSuccessfulSync(ResourceEnum.TokenSync, It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public void RecordSyncErrorTest()
        {
            // Act.
            _telemetryFacade.RecordSyncError(ResourceEnum.TokenSync, 500);
            _telemetryFacade.RecordSyncError(ResourceEnum.TelemetrySync, 500);
            _telemetryFacade.RecordSyncError(ResourceEnum.SplitSync, 500);

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordSyncError(ResourceEnum.TokenSync, 500), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordSyncError(ResourceEnum.TelemetrySync, 500), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordSyncError(ResourceEnum.SplitSync, 500), Times.Once);
            _telemetryStorage.Verify(mock => mock.RecordSyncError(ResourceEnum.SegmentSync, It.IsAny<int>()), Times.Never);
            _telemetryStorage.Verify(mock => mock.RecordSyncError(ResourceEnum.Impressionsync, It.IsAny<int>()), Times.Never);
            _telemetryStorage.Verify(mock => mock.RecordSyncError(ResourceEnum.EventSync, It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void RecordSyncLatencyTest()
        {
            // Act.
            _telemetryFacade.RecordSyncLatency(ResourceEnum.SplitSync, 342543);

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordSyncLatency(ResourceEnum.SplitSync, 22), Times.Once);
        }

        [TestMethod]
        public void RecordAuthRejectionsTest()
        {
            // Act.
            _telemetryFacade.RecordAuthRejections();
            _telemetryFacade.RecordAuthRejections();

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordAuthRejections(), Times.Exactly(2));
        }

        [TestMethod]
        public void RecordTokenRefreshesTest()
        {
            // Act.
            _telemetryFacade.RecordTokenRefreshes();
            _telemetryFacade.RecordTokenRefreshes();

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordTokenRefreshes(), Times.Exactly(2));
        }

        [TestMethod]
        public void RecordStreamingEventTest()
        {
            // Act.
            _telemetryFacade.RecordStreamingEvent(EventTypeEnum.OccupancyPri, 345);

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordStreamingEvent(It.IsAny<StreamingEvent>()), Times.Once);
        }

        [TestMethod]
        public void AddTagTest()
        {
            // Act.
            _telemetryFacade.AddTag("tag-1");

            // Assert.
            _telemetryStorage.Verify(mock => mock.AddTag("tag-1"), Times.Once);
        }

        [TestMethod]
        public void RecordSessionLengthTest()
        {
            // Act.
            _telemetryFacade.RecordSessionLength(123123);

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordSessionLength(123123), Times.Once);
        }
        
        [TestMethod]
        public void RecordBURTimeoutTest()
        {
            // Act.
            _telemetryFacade.RecordBURTimeout();
            _telemetryFacade.RecordBURTimeout();

            // Assert
            _telemetryStorage.Verify(mock => mock.RecordBURTimeout(), Times.Exactly(2));
        }

        [TestMethod]
        public void RecordNonReadyUsagesTest()
        {
            // Act.
            _telemetryFacade.RecordNonReadyUsages();

            // Assert.
            _telemetryStorage.Verify(mock => mock.RecordNonReadyUsages(), Times.Once);
        }
        #endregion

        #region Consumer Methods
        [TestMethod]
        public void PopLatenciesTest()
        {
            // Arrange.
            var defaultLatencies = new List<long>();
            var treatmentLatencies = new List<long> { 1, 2, 3, 4 };
            var treatmentsLatencies = new List<long> { 5, 6, 7 };
            var trackLatencies = new List<long> { 8, 9 };

            _telemetryStorage
                .Setup(mock => mock.PopLatencies())
                .Returns(new MethodLatencies
                {
                    Treatment = treatmentLatencies,
                    Treatments = treatmentsLatencies,
                    Track = trackLatencies,
                    TreatmenstWithConfig = defaultLatencies,
                    TreatmentWithConfig = defaultLatencies
                });

            // Act.
            var result = _telemetryFacade.PopLatencies();

            // Assert.
            _telemetryStorage.Verify(mock => mock.PopLatencies(), Times.Once);
            Assert.AreEqual(4, result.Treatment.Count);
            Assert.AreEqual(3, result.Treatments.Count);
            Assert.AreEqual(2, result.Track.Count);
            Assert.AreEqual(0, result.TreatmenstWithConfig.Count);
            Assert.AreEqual(0, result.TreatmentWithConfig.Count);
        }
        
        [TestMethod]
        public void PopExceptionsTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.PopExceptions())
                .Returns(new MethodExceptions
                {
                    Track = 1,
                    Treatment = 2,
                    Treatments = 3,
                });

            // Act.
            var result = _telemetryFacade.PopExceptions();

            // Assert.
            _telemetryStorage.Verify(mock => mock.PopExceptions(), Times.Once);
            Assert.AreEqual(1, result.Track);
            Assert.AreEqual(2, result.Treatment);
            Assert.AreEqual(3, result.Treatments);            
        }
        
        [TestMethod]
        public void GetImpressionsStatsTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped))
                .Returns(4);

            _telemetryStorage
                .Setup(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped))
                .Returns(8);

            // Act.
            var resultDeduped = _telemetryFacade.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped);
            var resultDropped = _telemetryFacade.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped);

            // Assert.
            _telemetryStorage.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped), Times.Once);
            _telemetryStorage.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped), Times.Once);
            _telemetryStorage.Verify(mock => mock.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued), Times.Never);
            Assert.AreEqual(4, resultDeduped);
            Assert.AreEqual(8, resultDropped);
        }

        [TestMethod]
        public void GetEventsStatsTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.GetEventsStats(EventsEnum.EventsDropped))
                .Returns(2);

            // Act.
            var result = _telemetryFacade.GetEventsStats(EventsEnum.EventsDropped);

            // Assert.
            _telemetryStorage.Verify(mock => mock.GetEventsStats(EventsEnum.EventsDropped), Times.Once);
            _telemetryStorage.Verify(mock => mock.GetEventsStats(EventsEnum.EventsQueued), Times.Never);
        }
        
        [TestMethod]
        public void GetLastSynchronizationsTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.GetLastSynchronizations())
                .Returns(new LastSynchronization
                {
                    Events = 3,
                    Segments = 12,
                    Splits = 6
                });

            // Act.
            var result = _telemetryFacade.GetLastSynchronizations();

            // Assert.
            _telemetryStorage.Verify(mock => mock.GetLastSynchronizations(), Times.Once);
            Assert.AreEqual(3, result.Events);
            Assert.AreEqual(6, result.Splits);
            Assert.AreEqual(12, result.Segments);
            Assert.AreEqual(0, result.Telemetry);
            Assert.AreEqual(0, result.Token);
            Assert.AreEqual(0, result.Impressions);
        }

        [TestMethod]
        public void PopHttpErrorsTest()
        {
            // Arrange.
            var eventsErrors = new Dictionary<int, long>
            {
                { 500, 4 },
                { 401, 7 }
            };

            _telemetryStorage
                .Setup(mock => mock.PopHttpErrors())
                .Returns(new HTTPErrors
                {
                    Events = eventsErrors,
                    Token = new Dictionary<int, long>(),
                    Telemetry = new Dictionary<int, long>(),
                    Splits = new Dictionary<int, long>(),
                    Segments = new Dictionary<int, long>(),
                    Impressions = new Dictionary<int, long>()
                });

            // Act.
            var result = _telemetryFacade.PopHttpErrors();

            // Assert.
            _telemetryStorage.Verify(mock => mock.PopHttpErrors(), Times.Once);
            Assert.AreEqual(4, result.Events[500]);
            Assert.AreEqual(7, result.Events[401]);
            Assert.AreEqual(0, result.Impressions.Keys.Count);
            Assert.AreEqual(0, result.Segments.Keys.Count);
            Assert.AreEqual(0, result.Splits.Keys.Count);
            Assert.AreEqual(0, result.Telemetry.Keys.Count);
            Assert.AreEqual(0, result.Token.Keys.Count);
        }

        [TestMethod]
        public void PopHttpLatenciesTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.PopHttpLatencies())
                .Returns(new HTTPLatencies
                {
                    Splits = new List<long> { 1, 2, 3 },
                    Impressions = new List<long> { 5, 8, 9, 10 },
                    Events = new List<long>(),
                    Segments = new List<long>(),
                    Telemetry = new List<long>(),
                    Token = new List<long>()
                });

            // Act.
            var result = _telemetryFacade.PopHttpLatencies();

            // Assert.
            _telemetryStorage.Verify(mock => mock.PopHttpLatencies(), Times.Once);
            Assert.AreEqual(3, result.Splits.Count);
            Assert.AreEqual(4, result.Impressions.Count);
            Assert.AreEqual(0, result.Events.Count);
            Assert.AreEqual(0, result.Segments.Count);
            Assert.AreEqual(0, result.Telemetry.Count);
            Assert.AreEqual(0, result.Token.Count);
        }

        [TestMethod]
        public void GetSplitsCountTest()
        {
            // Arrange.
            _splitCache
                .Setup(mock => mock.GetSplitNames())
                .Returns(new List<string>
                {
                    "split-1",
                    "split-2"
                });

            // Act.
            var result = _telemetryFacade.GetSplitsCount();

            // Assert.
            _splitCache.Verify(mock => mock.GetSplitNames(), Times.Once);
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void GetSegmentsCountTest()
        {
            // Arrange.
            _segmentCache
                .Setup(mock => mock.GetSegmentNames())
                .Returns(new List<string>
                {
                    "segment-1",
                    "segment-2",
                    "segment-3",
                });

            // Act.
            var result = _telemetryFacade.GetSegmentsCount();

            // Assert.
            _segmentCache.Verify(mock => mock.GetSegmentNames(), Times.Once);
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void GetSegmentKeysCountTest()
        {
            // Arrange.
            _segmentCache
                .Setup(mock => mock.GetSegmentNames())
                .Returns(new List<string>
                {
                    "segment-1",
                    "segment-2",
                });

            _segmentCache
                .Setup(mock => mock.GetSegmentKeys("segment-1"))
                .Returns(new List<string> { "key-1", "key-2", "key-3" });

            _segmentCache
                .Setup(mock => mock.GetSegmentKeys("segment-2"))
                .Returns(new List<string> { "key-4", "key-5" });

            // Act.
            var result = _telemetryFacade.GetSegmentKeysCount();

            // Assert.
            _segmentCache.Verify(mock => mock.GetSegmentNames(), Times.Once);
            _segmentCache.Verify(mock => mock.GetSegmentKeys("segment-1"), Times.Once);
            _segmentCache.Verify(mock => mock.GetSegmentKeys("segment-2"), Times.Once);
            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public void PopAuthRejectionsTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.PopAuthRejections())
                .Returns(5);

            // Act.
            var result = _telemetryFacade.PopAuthRejections();

            // Assert.
            _telemetryStorage.Verify(mock => mock.PopAuthRejections(), Times.Once);
            Assert.AreEqual(5, result);
        }
        
        [TestMethod]
        public void PopTokenRefreshesTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.PopTokenRefreshes())
                .Returns(6);

            // Act.
            var result = _telemetryFacade.PopTokenRefreshes();

            // Assert.
            _telemetryStorage.Verify(mock => mock.PopTokenRefreshes(), Times.Once);
            Assert.AreEqual(6, result);
        }
        
        [TestMethod]
        public void PopStreamingEventsTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.PopStreamingEvents())
                .Returns(new List<StreamingEvent>
                {
                    new StreamingEvent { Type = 1, Data = 123, Timestamp = 23423 },
                    new StreamingEvent { Type = 2, Data = 123, Timestamp = 23423 }
                });

            // Act.
            var result = _telemetryFacade.PopStreamingEvents();
            
            // Assert.
            _telemetryStorage.Verify(mock => mock.PopStreamingEvents(), Times.Once);
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void PopTagsTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.PopTags())
                .Returns(new List<string> { "tag-1", "tag-2" });

            // Act.
            var result = _telemetryFacade.PopTags();

            // Assert.
            _telemetryStorage.Verify(mock => mock.PopTags(), Times.Once);
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void GetSessionLengthTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.GetSessionLength())
                .Returns(3343);

            // Act.
            var result = _telemetryFacade.GetSessionLength();

            // Arrange.
            _telemetryStorage.Verify(mock => mock.GetSessionLength(), Times.Once);
            Assert.AreEqual(3343, result);
        }

        [TestMethod]
        public void GetBURTimeoutsTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.GetBURTimeouts())
                .Returns(456);

            // Act.
            var result = _telemetryFacade.GetBURTimeouts();

            // Arrange.
            _telemetryStorage.Verify(mock => mock.GetBURTimeouts(), Times.Once);
            Assert.AreEqual(456, result);
        }

        [TestMethod]
        public void GetNonReadyUsagesTest()
        {
            // Arrange.
            _telemetryStorage
                .Setup(mock => mock.GetNonReadyUsages())
                .Returns(53546);

            // Act.
            var result = _telemetryFacade.GetNonReadyUsages();

            // Arrange.
            _telemetryStorage.Verify(mock => mock.GetNonReadyUsages(), Times.Once);
            Assert.AreEqual(53546, result);
        }
        #endregion
    }
}
