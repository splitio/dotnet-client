using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class ImpressionsManagerTests
    {
        private readonly Mock<IImpressionsObserver> _impressionsObserver;
        private readonly Mock<IImpressionsLog> _impressionsLog;
        private readonly Mock<IImpressionListener> _customerImpressionListener;
        private readonly Mock<IImpressionsCounter> _impressionsCounter;
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly Mock<IUniqueKeysTracker> _uniqueKeysTracker;
        private readonly Mock<IStatusManager> _statusManager;
        private readonly ITasksManager _tasksManager;

        public ImpressionsManagerTests()
        {
            _impressionsObserver = new Mock<IImpressionsObserver>();
            _impressionsLog = new Mock<IImpressionsLog>();
            _customerImpressionListener = new Mock<IImpressionListener>();
            _impressionsCounter = new Mock<IImpressionsCounter>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _uniqueKeysTracker = new Mock<IUniqueKeysTracker>();
            _statusManager = new Mock<IStatusManager>();
            _tasksManager = new TasksManager(_statusManager.Object);
        }

        [TestMethod]
        public void BuildImpressionWithOptimizedAndWithPreviousTime()
        {
            // Arrange.
            var impressionsManager = GetManager(_customerImpressionListener.Object, labelsEnabled: true);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var ptTime = impTime - 150;

            _impressionsObserver
                .Setup(mock => mock.TestAndSet(It.IsAny<KeyImpression>()))
                .Returns(ptTime);

            // Act.
            var result = impressionsManager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key"));

            // Assert.
            Assert.AreEqual("matching-key", result.KeyName);
            Assert.AreEqual("feature", result.Feature);
            Assert.AreEqual("off", result.Treatment);
            Assert.AreEqual("label", result.Label);
            Assert.AreEqual("bucketing-key", result.BucketingKey);
            Assert.AreEqual(ptTime, result.PreviousTime);

            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Once);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Once);
        }

        [TestMethod]
        public void BuildImpressionWithDebugAndWithPreviousTime()
        {
            // Arrange.
            var impressionsManager = GetManager(_customerImpressionListener.Object, ImpressionsMode.Debug, labelsEnabled: true);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            _impressionsObserver
                .Setup(mock => mock.TestAndSet(It.IsAny<KeyImpression>()))
                .Returns((long?)null);

            // Act.
            var result = impressionsManager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key"));

            // Assert.
            Assert.AreEqual("matching-key", result.KeyName);
            Assert.AreEqual("feature", result.Feature);
            Assert.AreEqual("off", result.Treatment);
            Assert.AreEqual("label", result.Label);
            Assert.AreEqual("bucketing-key", result.BucketingKey);
            Assert.IsNull(result.PreviousTime);

            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Once);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Never);
        }

        [TestMethod]
        public void BuildImpressionWithDebugAndWithoutPreviousTime()
        {
            // Arrange.
            var impressionsManager = GetManager(_customerImpressionListener.Object, ImpressionsMode.Debug, addPt:false, labelsEnabled:true);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            // Act.
            var result = impressionsManager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key"));

            // Assert.
            Assert.AreEqual("matching-key", result.KeyName);
            Assert.AreEqual("feature", result.Feature);
            Assert.AreEqual("off", result.Treatment);
            Assert.AreEqual("label", result.Label);
            Assert.AreEqual("bucketing-key", result.BucketingKey);
            Assert.IsNull(result.PreviousTime);

            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Never);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Never);
        }

        [TestMethod]
        public void BuildAndTrack()
        {
            // Arrange.
            var impressionsManager = GetManager(_customerImpressionListener.Object, ImpressionsMode.Optimized);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            // Act.
            var imp = impressionsManager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key"));
            impressionsManager.Track(new List<KeyImpression> { imp });
           
            // Assert.
            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Once);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Never);

            Thread.Sleep(1000);
            _impressionsLog.Verify(mock => mock.Log(It.IsAny<List<KeyImpression>>()), Times.Once);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Once);
        }

        [TestMethod]
        public void BuildAndTrackWithoutCustomerListener()
        {
            // Arrange.
            var impressionsManager = GetManager(null, ImpressionsMode.Optimized);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            // Act.
            var imp = impressionsManager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key"));
            impressionsManager.Track(new List<KeyImpression> { imp });

            // Assert.
            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Once);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Never);

            Thread.Sleep(1000);
            _impressionsLog.Verify(mock => mock.Log(It.IsAny<List<KeyImpression>>()), Times.Once);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Never);
        }

        [TestMethod]
        public void Track_Optimized()
        {
            // Arrange.
            var impressionsManager = GetManager(_customerImpressionListener.Object, ImpressionsMode.Optimized);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                new KeyImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key", false, optimized: true),
                new KeyImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key", false, optimized: true)
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
            var impressionsManager = GetManager(_customerImpressionListener.Object, ImpressionsMode.Optimized);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                new KeyImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key", false, optimized: true),
                new KeyImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key", false, optimized: true)
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
            var impressionsManager = GetManager(_customerImpressionListener.Object, ImpressionsMode.Optimized);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                new KeyImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key", false, optimized: false),
                new KeyImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key", false, optimized: false)
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
            var impressionsManager = GetManager(_customerImpressionListener.Object, ImpressionsMode.Debug);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                new KeyImpression("matching-key", "feature", "off", impTime, 432543, "label", "bucketing-key", false),
                new KeyImpression("matching-key-2", "feature-2", "off", impTime, 432543, "label-2", "bucketing-key", false)
            };

            // Act.
            impressionsManager.Track(impressions);

            // Assert.
            Thread.Sleep(1000);
            _impressionsLog.Verify(mock => mock.Log(impressions), Times.Once);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Exactly(2));
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 0), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 2), Times.Once);
        }

        [TestMethod]
        public void TrackWithoutCustomerListener_Optimized()
        {
            // Arrange.
            var impressionsObserver = new ImpressionsObserver(new ImpressionHasher());
            var impressionsManager = GetManager(null, ImpressionsMode.Optimized, impressionsObserver: impressionsObserver);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                impressionsManager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key")),
                impressionsManager.Build(new TreatmentResult("feature-2", "label-2", "off", false, 432543, impTime: impTime), new Key("matching-key-2", "bucketing-key")),
                impressionsManager.Build(new TreatmentResult("feature-2", "label-2", "off", false, 432543, impTime: impTime), new Key("matching-key-2", "bucketing-key")),
                impressionsManager.Build(new TreatmentResult("feature-2", "label-2", "off", false, 432543, impTime: impTime), new Key("matching-key-2", "bucketing-key"))
            };

            var optimizedImpressions = impressions.Where(i => ImpressionsManager.ShouldQueueImpression(i)).ToList();

            // Act.
            impressionsManager.Track(impressions);

            // Assert.
            Thread.Sleep(1000);
            Assert.AreEqual(2, optimizedImpressions.Count);
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
            var impressionsManager = GetManager(null, ImpressionsMode.Debug, impressionsObserver: impressionsObserver);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();
            var impressions = new List<KeyImpression>
            {
                impressionsManager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key")),
                impressionsManager.Build(new TreatmentResult("feature-2", "label-2", "off", false, 432543, impTime: impTime), new Key("matching-key-2", "bucketing-key")),
                impressionsManager.Build(new TreatmentResult("feature-2", "label-2", "off", false, 432543, impTime: impTime), new Key("matching-key-2", "bucketing-key")),
                impressionsManager.Build(new TreatmentResult("feature-2", "label-2", "off", false, 432543, impTime: impTime), new Key("matching-key-2", "bucketing-key"))
            };

            // Act.
            impressionsManager.Track(impressions);

            // Assert.
            Thread.Sleep(1000);
            _impressionsLog.Verify(mock => mock.Log(impressions), Times.Once);
            _customerImpressionListener.Verify(mock => mock.Log(It.IsAny<KeyImpression>()), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, It.IsAny<int>()), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 4), Times.Once);
        }

        [TestMethod]
        public void BuildImpressionWithNoneMode()
        {
            // Arrange.
            var impressionsManager = GetManager(null, ImpressionsMode.None, labelsEnabled: true);
            
            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            // Act.
            var result = impressionsManager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key"));

            // Assert.
            Assert.AreEqual("matching-key", result.KeyName);
            Assert.AreEqual("feature", result.Feature);
            Assert.AreEqual("off", result.Treatment);
            Assert.AreEqual("label", result.Label);
            Assert.AreEqual("bucketing-key", result.BucketingKey);
            Assert.IsNull(result.PreviousTime);

            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Never);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Once);
            _uniqueKeysTracker.Verify(mock => mock.Track("matching-key", "feature"), Times.Once);
        }

        [TestMethod]
        public void BuildAndTrackWithNoneMode()
        {
            // Arrange
            var impressionsManager = GetManager(null, ImpressionsMode.None);
            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            // Act.
            var imp = impressionsManager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key"));
            impressionsManager.Track(new List<KeyImpression> { imp });

            // Assert.
            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Never);
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Once);
            _uniqueKeysTracker.Verify(mock => mock.Track("matching-key", "feature"), Times.Once);
        }

        [TestMethod]
        public void TrackWithNoneMode()
        {
            // Arrange.
            var impressionsManager = GetManager(null, ImpressionsMode.None);

            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            var impressions = new List<KeyImpression>
            {
                impressionsManager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key")),
                impressionsManager.Build(new TreatmentResult("feature-2", "label-2", "off", false, 432543, impTime: impTime), new Key("matching-key-2", "bucketing-key")),
                impressionsManager.Build(new TreatmentResult("feature-2", "label-2", "off", false, 432543, impTime: impTime), new Key("matching-key-2", "bucketing-key")),
                impressionsManager.Build(new TreatmentResult("feature-2", "label-2", "off", false, 432543, impTime: impTime), new Key("matching-key-2", "bucketing-key"))
            };

            // Act.
            impressionsManager.Track(impressions);

            // Assert.
            _impressionsLog.Verify(mock => mock.Log(It.IsAny<List<KeyImpression>>()), Times.Never);
        }

        [TestMethod]
        public async Task BuildAndTrackAsyncOptimized()
        {
            // Arrange.
            var manager = GetManager(_customerImpressionListener.Object, ImpressionsMode.Optimized, addPt: true, labelsEnabled: true);
            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            _impressionsObserver
                .SetupSequence(mock => mock.TestAndSet(It.IsAny<KeyImpression>()))
                .Returns((long?)null)
                .Returns(100);

            // Act.
            await manager.TrackAsync(new List<KeyImpression>
            {
                manager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key")),
                manager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key"))
            });

            // Assert.
            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Exactly(2));
            _impressionsCounter.Verify(mock => mock.Inc("feature", impTime), Times.Once);
            _impressionsLog.Verify(mock => mock.LogAsync(It.IsAny<List<KeyImpression>>()), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 2), Times.Once);
        }

        [TestMethod]
        public async Task BuildAndTrackAsyncNone()
        {
            // Arrange.
            var manager = GetManager(_customerImpressionListener.Object, ImpressionsMode.None, addPt: true, labelsEnabled: true);
            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            _impressionsObserver
                .SetupSequence(mock => mock.TestAndSet(It.IsAny<KeyImpression>()))
                .Returns((long?)null)
                .Returns(100);

            // Act.
            await manager.TrackAsync(new List<KeyImpression>
            {
                manager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key")),
                manager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key"))
            });

            // Assert.
            _impressionsCounter.Verify(mock => mock.Inc("feature", It.IsAny<long>()), Times.Exactly(2));
            _uniqueKeysTracker.Verify(mock => mock.Track("matching-key", "feature"), Times.Exactly(2));
            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Never);
            _impressionsLog.Verify(mock => mock.LogAsync(It.IsAny<List<KeyImpression>>()), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 0), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 0), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 2), Times.Never);
        }

        [TestMethod]
        public async Task BuildAndTrackAsyncDebug()
        {
            // Arrange.
            var manager = GetManager(_customerImpressionListener.Object, ImpressionsMode.Debug, addPt: true, labelsEnabled: true);
            var impTime = CurrentTimeHelper.CurrentTimeMillis();

            _impressionsObserver
                .SetupSequence(mock => mock.TestAndSet(It.IsAny<KeyImpression>()))
                .Returns((long?)null)
                .Returns(100);

            // Act.
            await manager.TrackAsync(new List<KeyImpression>
            {
                manager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key")),
                manager.Build(new TreatmentResult("feature", "label", "off", false, 432543, impTime: impTime), new Key("matching-key", "bucketing-key"))
            });

            // Assert.
            _impressionsCounter.Verify(mock => mock.Inc("feature", It.IsAny<long>()), Times.Never);
            _impressionsObserver.Verify(mock => mock.TestAndSet(It.IsAny<KeyImpression>()), Times.Exactly(2));
            _impressionsLog.Verify(mock => mock.LogAsync(It.IsAny<List<KeyImpression>>()), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsQueued, 2), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDropped, 0), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordImpressionsStats(ImpressionsEnum.ImpressionsDeduped, 0), Times.Never);
        }

        private ImpressionsManager GetManager(IImpressionListener impressionListener, ImpressionsMode mode = ImpressionsMode.Optimized, bool addPt = true, bool labelsEnabled = false, IImpressionsObserver impressionsObserver = null)
        {
            return new ImpressionsManager(_impressionsLog.Object,
                impressionListener,
                _impressionsCounter.Object,
                addPt,
                mode,
                _telemetryRuntimeProducer.Object,
                _tasksManager,
                _uniqueKeysTracker.Object,
                impressionsObserver ?? _impressionsObserver.Object,
                labelsEnabled);
        }
    }
}
