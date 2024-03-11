using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.EventSource;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.EventSource.Workers
{
    [TestClass]
    public class SplitsWorkerTests
    {
        private readonly Mock<ISynchronizer> _synchronizer;
        private readonly Mock<IFeatureFlagCache> _featureFlagCache;
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly Mock<ISelfRefreshingSegmentFetcher> _segmentFetcher;
        private readonly Mock<IStatusManager> _statusManager;
        private readonly Mock<IFeatureFlagSyncService> _featureFlagSyncService;
        private readonly BlockingCollection<SplitChangeNotification> _queue;

        private readonly ISplitsWorker _splitsWorker;

        public SplitsWorkerTests()
        {
            _synchronizer = new Mock<ISynchronizer>();
            _featureFlagCache = new Mock<IFeatureFlagCache>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _segmentFetcher = new Mock<ISelfRefreshingSegmentFetcher>();
            _statusManager = new Mock<IStatusManager>();
            _featureFlagSyncService = new Mock<IFeatureFlagSyncService>();
            _queue = new BlockingCollection<SplitChangeNotification>(new ConcurrentQueue<SplitChangeNotification>());

            var tasksManager = new TasksManager(_statusManager.Object);
            var task = tasksManager.NewPeriodicTask(Splitio.Enums.Task.FeatureFlagsWorker, 0);

            _splitsWorker = new SplitsWorker(_synchronizer.Object, _featureFlagCache.Object, _queue, _telemetryRuntimeProducer.Object, _segmentFetcher.Object, task, _featureFlagSyncService.Object);
        }

        [TestMethod]
        public void AddToQueueWithOldChangeNumberShouldNotFetch()
        {
            // Arrange.
            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(5);

            _splitsWorker.AddToQueue(new SplitChangeNotification
            {
                ChangeNumber = 2,
                PreviousChangeNumber = 1,
                FeatureFlag = new Split
                {
                    Status = "ARCHIVED",
                    Name = "mauro_ff",
                    DefaultTreatment = "off"
                }
            });

            // Act.
            _splitsWorker.Start();
            Thread.Sleep(1000);

            // Assert.
            _featureFlagCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<string>>(), It.IsAny<long>()), Times.Never);
            _synchronizer.Verify(mock => mock.SynchronizeSplitsAsync(It.IsAny<long>()), Times.Never);
            _featureFlagCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public void AddToQueueWithSegmentNameShouldFetchSegment()
        {
            // Arrange.
            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(5);

            _featureFlagSyncService
                .Setup(mock => mock.UpdateFeatureFlagsFromChanges(It.IsAny<List<Split>>(), It.IsAny<long>()))
                .Returns(new List<string> { "segment-name" });

            _splitsWorker.AddToQueue(new SplitChangeNotification
            {
                ChangeNumber = 6,
                PreviousChangeNumber = 5,
                FeatureFlag = new Split
                {
                    Status = "ACTIVE",
                    Name = "mauro_ff",
                    DefaultTreatment = "off",
                    Conditions = new List<Condition>()
                    { 
                        new Condition()
                        {
                            ConditionType = "",
                            MatcherGroup = new MatcherGroup()
                            {
                                Matchers = new List<Matcher>
                                {
                                    new Matcher()
                                    {
                                        UserDefinedSegmentMatcherData = new UserDefinedSegmentData()
                                        {
                                            segmentName = "segment-test"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

            // Act.
            _splitsWorker.Start();
            Thread.Sleep(1000);

            // Assert.
            _featureFlagSyncService.Verify(mock => mock.UpdateFeatureFlagsFromChanges(It.IsAny<List<Split>>(), It.IsAny<long>()), Times.Once);
            _featureFlagCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<string>>(), It.IsAny<long>()), Times.Never);
            _synchronizer.Verify(mock => mock.SynchronizeSplitsAsync(It.IsAny<long>()), Times.Never);
            _featureFlagCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordUpdatesFromSSE(UpdatesFromSSEEnum.Splits), Times.Once);
            _segmentFetcher.Verify(mock => mock.FetchSegmentsIfNotExistsAsync(It.IsAny<List<string>>()), Times.Once);
        }

        [TestMethod]
        public void AddToQueueWithNewFormatAndSamePcnShouldUpdateInMemory()
        {
            // Arrange.
            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(5);

            _splitsWorker.AddToQueue(new SplitChangeNotification
            {
                ChangeNumber = 6,
                PreviousChangeNumber = 5,
                FeatureFlag = new Split
                {
                    Status = "ACTIVE",
                    Name = "mauro_ff",
                    DefaultTreatment = "off",
                    Conditions = new List<Condition>()
                }
            });

            _featureFlagSyncService
                .Setup(mock => mock.UpdateFeatureFlagsFromChanges(It.IsAny<List<Split>>(), It.IsAny<long>()))
                .Returns(new List<string>());

            // Act.
            _splitsWorker.Start();
            Thread.Sleep(1000);

            // Assert.
            _featureFlagSyncService.Verify(mock => mock.UpdateFeatureFlagsFromChanges(It.IsAny<List<Split>>(), It.IsAny<long>()), Times.Once);
            _featureFlagCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<string>>(), It.IsAny<long>()), Times.Never);
            _synchronizer.Verify(mock => mock.SynchronizeSplitsAsync(It.IsAny<long>()), Times.Never);
            _featureFlagCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordUpdatesFromSSE(UpdatesFromSSEEnum.Splits), Times.Once);
        }

        [TestMethod]
        public void AddToQueueSamePcnShouldRemoveSplit()
        {
            // Arrange.
            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(1);

            _splitsWorker.AddToQueue(new SplitChangeNotification
            {
                ChangeNumber = 2,
                PreviousChangeNumber = 1,
                FeatureFlag = new Split
                {
                    Status = "ARCHIVED",
                    Name = "mauro_ff",
                    DefaultTreatment = "off"
                }
            });

            _featureFlagSyncService
                .Setup(mock => mock.UpdateFeatureFlagsFromChanges(It.IsAny<List<Split>>(), It.IsAny<long>()))
                .Returns(new List<string>());

            // Act.
            _splitsWorker.Start();
            Thread.Sleep(1000);

            // Assert.
            _featureFlagSyncService.Verify(mock => mock.UpdateFeatureFlagsFromChanges(It.IsAny<List<Split>>(), It.IsAny<long>()), Times.Once);
            _featureFlagCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<string>>(), It.IsAny<long>()), Times.Never);
            _featureFlagCache.Verify(mock => mock.SetChangeNumber(2), Times.Never);

        }

        [TestMethod]        
        public async Task AddToQueue_WithElements_ShouldTriggerFetch()
        {
            // Act.
            _splitsWorker.Start();
            _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698457 });
            _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698467 });
            _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698477 });
            _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698476 });
            Thread.Sleep(1000);

            // Assert
            _synchronizer.Verify(mock => mock.SynchronizeSplitsAsync(It.IsAny<long>()), Times.Exactly(4));

            await _splitsWorker.StopAsync();
            _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698486 });
            Thread.Sleep(1000);
            _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698496 });

            // Assert
            _synchronizer.Verify(mock => mock.SynchronizeSplitsAsync(It.IsAny<long>()), Times.Exactly(4));
        }

        [TestMethod]
        public void AddToQueue_WithoutElemts_ShouldNotTriggerFetch()
        {
            // Act.
            _splitsWorker.Start();
            Thread.Sleep(1000);

            // Assert.
            _synchronizer.Verify(mock => mock.SynchronizeSplitsAsync(It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public void Kill_ShouldTriggerFetch()
        {
            // Arrange.            
            var changeNumber = 1585956698457;
            var splitName = "split-test";
            var defaultTreatment = "off";

            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(1585956698447);

            _splitsWorker.Start();

            // Act.            
            _splitsWorker.Kill(new SplitKillNotification
            {
                ChangeNumber = changeNumber,
                SplitName = splitName,
                DefaultTreatment = defaultTreatment
            });
            Thread.Sleep(1000);

            // Assert.
            _featureFlagCache.Verify(mock => mock.Kill(changeNumber, splitName, defaultTreatment), Times.Once);
        }

        [TestMethod]
        public void Kill_ShouldNotTriggerFetch()
        {
            // Arrange.            
            var changeNumber = 1585956698457;
            var splitName = "split-test";
            var defaultTreatment = "off";

            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(1585956698467);

            _splitsWorker.Start();

            // Act.            
            _splitsWorker.Kill(new SplitKillNotification
            {
                ChangeNumber = changeNumber,
                SplitName = splitName,
                DefaultTreatment = defaultTreatment
            });
            Thread.Sleep(1000);

            // Assert.
            _featureFlagCache.Verify(mock => mock.Kill(changeNumber, splitName, defaultTreatment), Times.Never);
        }
    }
}
