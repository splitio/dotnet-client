using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Filters;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Shared
{
    [TestClass]
    public class ClientExtensionServiceTests
    {
        private readonly Mock<IBlockUntilReadyService> _blockUntilReadyService;
        private readonly Mock<IStatusManager> _statusManager;
        private readonly Mock<ITelemetryEvaluationProducer> _telemetryEvaluationProducer;
        private readonly Mock<ISplitLogger> _logger;
        private readonly Mock<IFeatureFlagCacheConsumer> _featureFlagCacheConsumer;
        private readonly ISplitNameValidator _splitNameValidator;
        private readonly IKeyValidator _keyValidator;
        private readonly IEventTypeValidator _eventTypeValidator;
        private readonly IEventPropertiesValidator _eventPropertiesValidator;
        private readonly ITrafficTypeValidator _trafficTypeValidator;
        private readonly IFlagSetsValidator _flagSetsValidator;
        private readonly IFlagSetsFilter _flagSetsFilter;

        private readonly IClientExtensionService _service;

        public ClientExtensionServiceTests()
        {
            _blockUntilReadyService = new Mock<IBlockUntilReadyService>();
            _statusManager = new Mock<IStatusManager>();
            _logger = new Mock<ISplitLogger>();
            _telemetryEvaluationProducer = new Mock<ITelemetryEvaluationProducer>();
            _featureFlagCacheConsumer = new Mock<IFeatureFlagCacheConsumer>();
            _splitNameValidator = new SplitNameValidator();
            _keyValidator = new KeyValidator(_logger.Object);
            _eventTypeValidator = new EventTypeValidator(_logger.Object);
            _eventPropertiesValidator = new EventPropertiesValidator(_logger.Object);
            _trafficTypeValidator = new TrafficTypeValidator(_featureFlagCacheConsumer.Object, _blockUntilReadyService.Object, _logger.Object);
            _flagSetsValidator = new FlagSetsValidator();
            _flagSetsFilter = new FlagSetsFilter(new HashSet<string>());

            _service = new ClientExtensionService(_blockUntilReadyService.Object, _statusManager.Object, _keyValidator, _splitNameValidator, _telemetryEvaluationProducer.Object, _eventTypeValidator, _eventPropertiesValidator, _trafficTypeValidator, _flagSetsValidator, _flagSetsFilter);
        }

        [TestMethod]
        public void RecordTelemetryGetTreatments()
        {
            // Act.
            _service.RecordException(Splitio.Enums.API.GetTreatments);
            _service.RecordException(Splitio.Enums.API.GetTreatmentsAsync);
            _service.RecordLatency(Splitio.Enums.API.GetTreatments, 10);
            _service.RecordLatency(Splitio.Enums.API.GetTreatmentsAsync, 10);

            // Assert.
            _telemetryEvaluationProducer.Verify(mock => mock.RecordException(MethodEnum.Treatments), Times.Exactly(2));
            _telemetryEvaluationProducer.Verify(mock => mock.RecordLatency(MethodEnum.Treatments, 6), Times.Exactly(2));
        }

        [TestMethod]
        public void RecordTelemetryGetTreatment()
        {
            // Act.
            _service.RecordException(Splitio.Enums.API.GetTreatment);
            _service.RecordException(Splitio.Enums.API.GetTreatmentAsync);
            _service.RecordLatency(Splitio.Enums.API.GetTreatment, 10);
            _service.RecordLatency(Splitio.Enums.API.GetTreatmentAsync, 10);

            // Assert.
            _telemetryEvaluationProducer.Verify(mock => mock.RecordException(MethodEnum.Treatment), Times.Exactly(2));
            _telemetryEvaluationProducer.Verify(mock => mock.RecordLatency(MethodEnum.Treatment, 6), Times.Exactly(2));
        }

        [TestMethod]
        public void RecordTelemetryGetTreatmentWithConfig()
        {
            // Act.
            _service.RecordException(Splitio.Enums.API.GetTreatmentWithConfig);
            _service.RecordException(Splitio.Enums.API.GetTreatmentWithConfigAsync);
            _service.RecordLatency(Splitio.Enums.API.GetTreatmentWithConfig, 10);
            _service.RecordLatency(Splitio.Enums.API.GetTreatmentWithConfigAsync, 10);

            // Assert.
            _telemetryEvaluationProducer.Verify(mock => mock.RecordException(MethodEnum.TreatmentWithConfig), Times.Exactly(2));
            _telemetryEvaluationProducer.Verify(mock => mock.RecordLatency(MethodEnum.TreatmentWithConfig, 6), Times.Exactly(2));
        }

        [TestMethod]
        public void RecordTelemetryGetTreatmentsWithConfig()
        {
            // Act.
            _service.RecordException(Splitio.Enums.API.GetTreatmentsWithConfig);
            _service.RecordException(Splitio.Enums.API.GetTreatmentsWithConfigAsync);
            _service.RecordLatency(Splitio.Enums.API.GetTreatmentsWithConfig, 10);
            _service.RecordLatency(Splitio.Enums.API.GetTreatmentsWithConfigAsync, 10);

            // Assert.
            _telemetryEvaluationProducer.Verify(mock => mock.RecordException(MethodEnum.TreatmentsWithConfig), Times.Exactly(2));
            _telemetryEvaluationProducer.Verify(mock => mock.RecordLatency(MethodEnum.TreatmentsWithConfig, 6), Times.Exactly(2));
        }

        [TestMethod]
        public void RecordTelemetryTrack()
        {
            // Act.
            _service.RecordException(Splitio.Enums.API.Track);
            _service.RecordException(Splitio.Enums.API.TrackAsync);
            _service.RecordLatency(Splitio.Enums.API.Track, 10);
            _service.RecordLatency(Splitio.Enums.API.TrackAsync, 10);

            // Assert.
            _telemetryEvaluationProducer.Verify(mock => mock.RecordException(MethodEnum.Track), Times.Exactly(2));
            _telemetryEvaluationProducer.Verify(mock => mock.RecordLatency(MethodEnum.Track, 6), Times.Exactly(2));
        }

        [TestMethod]
        public void TreatmentsValidationsReady()
        {
            // Arrange.
            var featureFlagNames = new List<string>
            {
                "feature-flag",
                string.Empty
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act.
            var names = _service.TreatmentsValidations(Splitio.Enums.API.GetTreatment, new Key("matchingKey", "bucketingKey"), featureFlagNames, _logger.Object, out var result);

            // Assert.
            Assert.IsNull(result);
            Assert.IsTrue(names.Count == 1);
        }

        [TestMethod]
        public void TreatmentsValidationsNotReady()
        {
            // Arrange.
            var featureFlagNames = new List<string>
            {
                "feature-flag",
                string.Empty
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(false);

            // Act.
            var names = _service.TreatmentsValidations(Splitio.Enums.API.GetTreatment, new Key("matchingKey", "bucketingKey"), featureFlagNames, _logger.Object, out var result);

            // Assert.
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(0, names.Count);
        }

        [TestMethod]
        public void TreatmentValidationsReady()
        {
            // Arrange.
            var expected = "feature-flag";

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act.
            var success = _service.TreatmentValidations(Splitio.Enums.API.GetTreatment, new Key("matchingKey", "bucketingKey"), expected, _logger.Object, out var result);

            // Assert.
            Assert.IsTrue(success);
            Assert.AreEqual("feature-flag", result);
        }

        [TestMethod]
        public void TreatmentValidationsNotReady()
        {
            // Arrange.
            var expected = "feature-flag";

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(false);

            // Act.
            var success = _service.TreatmentValidations(Splitio.Enums.API.GetTreatment, new Key("matchingKey", "bucketingKey"), expected, _logger.Object, out var result);

            // Assert.
            Assert.IsFalse(success);
            Assert.IsNull(result);
            _logger.Verify(mock => mock.Warn($"GetTreatment: the SDK is not ready, results may be incorrect for feature flag {expected}. Make sure to wait for SDK readiness before using this method"), Times.Once);
        }

        [TestMethod]
        public void FlagSetsValidationsWithMultipleValuesReturnsFiltered()
        {
            // Arrange.
            var key = new Key("match-key", null);
            var flagSets = new List<string>
            {
                "flag-1",
                "flag2",
                "flag2",
                "flag_set_3",
                "FLAG4",
                null,
                string.Empty,
                "flagsetlkajsdlkjaslkdjlkasjdklajskldjaskljdlkasjdkljaskldjlkasjdkljaslkjdlkasjdlkjaslkjdlkasjldkjasdsf"
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act.
            var result = _service.FlagSetsValidations(Splitio.Enums.API.GetTreatment, key, flagSets, _logger.Object);

            // Assert.
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("flag2"));
            Assert.IsTrue(result.Contains("flag_set_3"));
            Assert.IsTrue(result.Contains("flag4"));
        }

        [TestMethod]
        public void FlagSetsValidationsWithSetsInConfig()
        {
            // Arrange.
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string> { "set1", "set2" });
            var service = new ClientExtensionService(_blockUntilReadyService.Object, _statusManager.Object, _keyValidator, _splitNameValidator, _telemetryEvaluationProducer.Object, _eventTypeValidator, _eventPropertiesValidator, _trafficTypeValidator, _flagSetsValidator, flagSetsFilter);

            var key = new Key("match-key", null);
            var flagSets = new List<string> { "set5", "set4", "set3", "set2", "set1", "set" };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act.
            var result = service.FlagSetsValidations(Splitio.Enums.API.GetTreatment, key, flagSets, _logger.Object);

            // Assert.
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("set1"));
            Assert.IsTrue(result.Contains("set2"));
        }
    }
}
