using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.EventSource;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
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
        private readonly Mock<IUpdater<Split>> _featureFlagSyncService;
        private readonly Mock<IRuleBasedSegmentCache> _rbsCache;
        private readonly Mock<IUpdater<RuleBasedSegmentDto>> _rbsUpdater;

        private readonly ISplitsWorker _splitsWorker;

        public SplitsWorkerTests()
        {
            _synchronizer = new Mock<ISynchronizer>();
            _featureFlagCache = new Mock<IFeatureFlagCache>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _segmentFetcher = new Mock<ISelfRefreshingSegmentFetcher>();
            _featureFlagSyncService = new Mock<IUpdater<Split>>();
            _rbsCache = new Mock<IRuleBasedSegmentCache>();
            _rbsUpdater = new Mock<IUpdater<RuleBasedSegmentDto>>();


            _splitsWorker = new SplitsWorker(_synchronizer.Object, _featureFlagCache.Object, _telemetryRuntimeProducer.Object, _segmentFetcher.Object, _featureFlagSyncService.Object, _rbsCache.Object, _rbsUpdater.Object);
        }

        [TestMethod]
        public async Task AddToQueueWithOldChangeNumberShouldNotFetch()
        {
            // Arrange.
            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(5);

            await _splitsWorker.AddToQueue(new SplitChangeNotification
            {
                ChangeNumber = 2,
                PreviousChangeNumber = 1,
                FeatureFlag = new Split
                {
                    status = "ARCHIVED",
                    name = "mauro_ff",
                    defaultTreatment = "off"
                }
            });

            // Act.
            _splitsWorker.Start();
            await Task.Delay(1000);

            // Assert.
            _featureFlagCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<string>>(), It.IsAny<long>()), Times.Never);
            _synchronizer.Verify(mock => mock.SynchronizeSplitsAsync(It.IsAny<long>()), Times.Never);
            _featureFlagCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public async Task AddToQueueWithSegmentNameShouldFetchSegment()
        {
            // Arrange.
            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(5);

            _featureFlagSyncService
                .Setup(mock => mock.Process(It.IsAny<List<Split>>(), It.IsAny<long>()))
                .Returns(new Dictionary<Splitio.Enums.SegmentType, List<string>>
                { 
                    { Splitio.Enums.SegmentType.Standard, new List<string> { "segment-name" } },
                    { Splitio.Enums.SegmentType.RuleBased, new List<string>() }
                });

            // Act.
            _splitsWorker.Start();
            await _splitsWorker.AddToQueue(new SplitChangeNotification
            {
                ChangeNumber = 6,
                PreviousChangeNumber = 5,
                FeatureFlag = new Split
                {
                    status = "ACTIVE",
                    name = "mauro_ff",
                    defaultTreatment = "off",
                    conditions = new List<ConditionDefinition>()
                    {
                        new ConditionDefinition()
                        {
                            conditionType = "",
                            matcherGroup = new MatcherGroupDefinition()
                            {
                                matchers = new List<MatcherDefinition>
                                {
                                    new MatcherDefinition()
                                    {
                                        userDefinedSegmentMatcherData = new UserDefinedSegmentData()
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
            await Task.Delay(1000);

            // Assert.
            _featureFlagSyncService.Verify(mock => mock.Process(It.IsAny<List<Split>>(), It.IsAny<long>()), Times.Once);
            _featureFlagCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<string>>(), It.IsAny<long>()), Times.Never);
            _synchronizer.Verify(mock => mock.SynchronizeSplitsAsync(It.IsAny<long>()), Times.Never);
            _featureFlagCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordUpdatesFromSSE(UpdatesFromSSEEnum.Splits), Times.Once);
            _segmentFetcher.Verify(mock => mock.FetchSegmentsIfNotExistsAsync(It.IsAny<List<string>>()), Times.Once);
        }

        [TestMethod]
        public async Task AddToQueueWithNewFormatAndSamePcnShouldUpdateInMemory()
        {
            // Arrange.
            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(5);

            _featureFlagSyncService
                .Setup(mock => mock.Process(It.IsAny<List<Split>>(), It.IsAny<long>()))
                .Returns(new Dictionary<Splitio.Enums.SegmentType, List<string>>
                {
                    { Splitio.Enums.SegmentType.Standard, new List<string>() },
                    { Splitio.Enums.SegmentType.RuleBased, new List<string>() }
                });

            // Act.
            _splitsWorker.Start();
            await _splitsWorker.AddToQueue(new SplitChangeNotification
            {
                ChangeNumber = 6,
                PreviousChangeNumber = 5,
                FeatureFlag = new Split
                {
                    status = "ACTIVE",
                    name = "mauro_ff",
                    defaultTreatment = "off",
                    conditions = new List<ConditionDefinition>()
                }
            });
            await Task.Delay(1000);

            // Assert.
            _featureFlagSyncService.Verify(mock => mock.Process(It.IsAny<List<Split>>(), It.IsAny<long>()), Times.Once);
            _featureFlagCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<string>>(), It.IsAny<long>()), Times.Never);
            _synchronizer.Verify(mock => mock.SynchronizeSplitsAsync(It.IsAny<long>()), Times.Never);
            _featureFlagCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Never);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordUpdatesFromSSE(UpdatesFromSSEEnum.Splits), Times.Once);
        }

        [TestMethod]
        public async Task AddToQueueSamePcnShouldRemoveSplit()
        {
            // Arrange.
            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(1);

            _featureFlagSyncService
                .Setup(mock => mock.Process(It.IsAny<List<Split>>(), It.IsAny<long>()))
                .Returns(new Dictionary<Splitio.Enums.SegmentType, List<string>>
                {
                    { Splitio.Enums.SegmentType.Standard, new List<string>() },
                    { Splitio.Enums.SegmentType.RuleBased, new List<string>() }
                });

            // Act.
            _splitsWorker.Start();
            await _splitsWorker.AddToQueue(new SplitChangeNotification
            {
                ChangeNumber = 2,
                PreviousChangeNumber = 1,
                FeatureFlag = new Split
                {
                    status = "ARCHIVED",
                    name = "mauro_ff",
                    defaultTreatment = "off"
                }
            });
            await Task.Delay(1000);

            // Assert.
            _featureFlagSyncService.Verify(mock => mock.Process(It.IsAny<List<Split>>(), It.IsAny<long>()), Times.Once);
            _featureFlagCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<string>>(), It.IsAny<long>()), Times.Never);
            _featureFlagCache.Verify(mock => mock.SetChangeNumber(2), Times.Never);

        }

        [TestMethod]        
        public async Task AddToQueue_WithElements_ShouldTriggerFetch()
        {
            // Act.
            _splitsWorker.Start();
            await _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698457 });
            await _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698467 });
            await _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698477 });
            await _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698476 });
            await Task.Delay(1000);

            // Assert
            _synchronizer.Verify(mock => mock.SynchronizeSplitsAsync(It.IsAny<long>()), Times.Exactly(4));

            _splitsWorker.Stop();
            await _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698486 });
            await Task.Delay(1000);
            await _splitsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = 1585956698496 });

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
