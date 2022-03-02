using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class ImpressionsManagerTests
    {
        private readonly WrapperAdapter wrapperAdapter = new WrapperAdapter();

        private readonly Mock<IImpressionsObserver> _impressionsObserver;
        private readonly Mock<IImpressionsLog> _impressionsLog;
        private readonly Mock<IImpressionListener> _customerImpressionListener;
        private readonly Mock<IImpressionsCounter> _impressionsCounter;
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly Mock<IUniqueKeysTracker> _uniqueKeysTracker;
        private readonly ITasksManager _tasksManager;

        public ImpressionsManagerTests()
        {
            _impressionsObserver = new Mock<IImpressionsObserver>();
            _impressionsLog = new Mock<IImpressionsLog>();
            _customerImpressionListener = new Mock<IImpressionListener>();
            _impressionsCounter = new Mock<IImpressionsCounter>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _uniqueKeysTracker = new Mock<IUniqueKeysTracker>();

            _tasksManager = new TasksManager(new WrapperAdapter());
        }

        [TestMethod]
        public void BuildImpressionWithOptimizedAndWithPreviousTime()
        {
            // Arrange.
            var impressionsManager = new ImpressionsManager(_impressionsLog.Object,
                _customerImpressionListener.Object,
                _impressionsCounter.Object,
                true,
                ImpressionsMode.Optimized,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                _impressionsObserver.Object);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var ptTime = impTime - 150;

            _impressionsObserver
                .Setup(mock => mock.TestAndSet(It.IsAny<KeyImpression>()))
                .Returns(ptTime);

            // Act.
            var result = impressionsManager.BuildImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key");

            // Assert.
            Assert.AreEqual("matching-key", result.keyName);
            Assert.AreEqual("feature", result.feature);
            Assert.AreEqual("off", result.treatment);
            Assert.AreEqual(impTime, result.time);
            Assert.AreEqual("label", result.label);
            Assert.AreEqual("bucketing-key", result.bucketingKey);
            Assert.AreEqual(ptTime, result.previousTime);

            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Once);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Once);
        }

        [TestMethod]
        public void BuildImpressionWithDebugAndWithPreviousTime()
        {
            // Arrange.
            var impressionsManager = new ImpressionsManager(_impressionsLog.Object,
                _customerImpressionListener.Object,
                _impressionsCounter.Object,
                true,
                ImpressionsMode.Debug,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                _impressionsObserver.Object);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            _impressionsObserver
                .Setup(mock => mock.TestAndSet(It.IsAny<KeyImpression>()))
                .Returns((long?)null);

            // Act.
            var result = impressionsManager.BuildImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key");

            // Assert.
            Assert.AreEqual("matching-key", result.keyName);
            Assert.AreEqual("feature", result.feature);
            Assert.AreEqual("off", result.treatment);
            Assert.AreEqual(impTime, result.time);
            Assert.AreEqual("label", result.label);
            Assert.AreEqual("bucketing-key", result.bucketingKey);
            Assert.IsNull(result.previousTime);

            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Once);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Never);
        }

        [TestMethod]
        public void BuildImpressionWithDebugAndWithoutPreviousTime()
        {
            // Arrange.
            var impressionsManager = new ImpressionsManager(_impressionsLog.Object,
                _customerImpressionListener.Object,
                _impressionsCounter.Object,
                false,
                ImpressionsMode.Debug,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                _impressionsObserver.Object);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            // Act.
            var result = impressionsManager.BuildImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key");

            // Assert.
            Assert.AreEqual("matching-key", result.keyName);
            Assert.AreEqual("feature", result.feature);
            Assert.AreEqual("off", result.treatment);
            Assert.AreEqual(impTime, result.time);
            Assert.AreEqual("label", result.label);
            Assert.AreEqual("bucketing-key", result.bucketingKey);
            Assert.IsNull(result.previousTime);

            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Never);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Never);
        }

        [TestMethod]
        public void BuildAndTrack()
        {
            // Arrange.
            var impressionsManager = new ImpressionsManager(_impressionsLog.Object,
                _customerImpressionListener.Object,
                _impressionsCounter.Object,
                true,
                ImpressionsMode.Optimized,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                _impressionsObserver.Object);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            // Act.
            impressionsManager.BuildAndTrack("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key");
           
            // Assert.
            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Once);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Once);

            Thread.Sleep(1000);
            _impressionsLog.Verify(mock => mock.Log(It.IsAny<List<KeyImpression>>()), Times.Once);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Once);
        }

        [TestMethod]
        public void BuildAndTrackWithoutCustomerListener()
        {
            // Arrange.
            var impressionsManager = new ImpressionsManager(_impressionsLog.Object,
                null,
                _impressionsCounter.Object,
                true,
                ImpressionsMode.Optimized,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                _impressionsObserver.Object);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            // Act.
            impressionsManager.BuildAndTrack("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key");

            // Assert.
            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Once);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Once);

            Thread.Sleep(1000);
            _impressionsLog.Verify(mock => mock.Log(It.IsAny<List<KeyImpression>>()), Times.Once);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Never);
        }

        [TestMethod]
        public void Track_Optimized()
        {
            // Arrange.
            var impressionsManager = new ImpressionsManager(_impressionsLog.Object,
                _customerImpressionListener.Object,
                _impressionsCounter.Object,
                true,
                ImpressionsMode.Optimized,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                _impressionsObserver.Object);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                new KeyImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key", optimized: true),
                new KeyImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key", optimized: true)
            };

            // Act.
            impressionsManager.Track(impressions);

            // Assert.
            Thread.Sleep(1000);
            _impressionsLog.Verify(mock => mock.Log(impressions), Times.Once);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Exactly(2));
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 2), Times.Once);
        }

        [TestMethod]
        public void Track_Optimized_WithOneImpressionDropped()
        {
            // Arrange.
            var impressionsManager = new ImpressionsManager(_impressionsLog.Object,
                _customerImpressionListener.Object,
                _impressionsCounter.Object,
                true,
                ImpressionsMode.Optimized,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                _impressionsObserver.Object);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                new KeyImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key", optimized: true),
                new KeyImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key", optimized: true)
            };

            _impressionsLog
                .Setup(mock => mock.Log(It.IsAny<List<KeyImpression>>()))
                .Returns(1);

            // Act.
            impressionsManager.Track(impressions);

            // Assert.
            Thread.Sleep(1000);
            _impressionsLog.Verify(mock => mock.Log(impressions), Times.Once);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Exactly(2));
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 1), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 1), Times.Once);
        }

        [TestMethod]
        public void Track_Optimized_ShouldnotLog()
        {
            // Arrange.
            var impressionsManager = new ImpressionsManager(_impressionsLog.Object,
                _customerImpressionListener.Object,
                _impressionsCounter.Object,
                true,
                ImpressionsMode.Optimized,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                _impressionsObserver.Object);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                new KeyImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key", optimized: false),
                new KeyImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key", optimized: false)
            };

            // Act.
            impressionsManager.Track(impressions);

            // Assert.
            Thread.Sleep(1000);
            _impressionsLog.Verify(mock => mock.Log(impressions), Times.Never);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Exactly(2));
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 2), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 0), Times.Once);
        }

        [TestMethod]
        public void Track_Debug()
        {
            // Arrange.
            var impressionsManager = new ImpressionsManager(_impressionsLog.Object,
                _customerImpressionListener.Object,
                _impressionsCounter.Object,
                true,
                ImpressionsMode.Debug,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                _impressionsObserver.Object);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                new KeyImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key"),
                new KeyImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key")
            };

            // Act.
            impressionsManager.Track(impressions);

            // Assert.
            Thread.Sleep(1000);
            _impressionsLog.Verify(mock => mock.Log(impressions), Times.Once);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Exactly(2));
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 2), Times.Once);
        }

        [TestMethod]
        public void TrackWithoutCustomerListener_Optimized()
        {
            // Arrange.
            var impressionsObserver = new ImpressionsObserver(new ImpressionHasher());
            var impressionsCounter = new ImpressionsCounter();
            var impressionsManager = new ImpressionsManager(_impressionsLog.Object,
                null,
                impressionsCounter,
                true,
                ImpressionsMode.Optimized,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                impressionsObserver);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                impressionsManager.BuildImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key"),
                impressionsManager.BuildImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key"),
                impressionsManager.BuildImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key"),
                impressionsManager.BuildImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key")
            };

            var optimizedImpressions = impressions.Where(i => impressionsManager.ShouldQueueImpression(i)).ToList();

            // Act.
            impressionsManager.Track(impressions);

            // Assert.
            Thread.Sleep(1000);
            Assert.AreEqual(2, optimizedImpressions.Count());
            _impressionsLog.Verify(mock => mock.Log(optimizedImpressions), Times.Once);
            _impressionsLog.Verify(mock => mock.Log(impressions), Times.Never);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 2), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 2), Times.Once);
        }

        [TestMethod]
        public void TrackWithoutCustomerListener_Debug()
        {
            // Arrange.
            var impressionsObserver = new ImpressionsObserver(new ImpressionHasher());
            var impressionsCounter = new ImpressionsCounter();
            var impressionsManager = new ImpressionsManager(_impressionsLog.Object,
                null,
                impressionsCounter,
                true,
                ImpressionsMode.Debug,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                impressionsObserver);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                impressionsManager.BuildImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key"),
                impressionsManager.BuildImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key"),
                impressionsManager.BuildImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key"),
                impressionsManager.BuildImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key")
            };

            // Act.
            impressionsManager.Track(impressions);

            // Assert.
            Thread.Sleep(1000);
            _impressionsLog.Verify(mock => mock.Log(impressions), Times.Once);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 4), Times.Once);
        }
    }
}
