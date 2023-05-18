using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EventSource;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.Parsing;
using Splitio.Services.Parsing.Interfaces;

namespace Splitio_Tests.Unit_Tests.EventSource
{
    [TestClass]
    public class NotificationPorcessorTests
    {
        private readonly Mock<ISplitCache> _featureFlagCache;
        private readonly Mock<ISplitsWorker> _splitsWorker;
        private readonly Mock<ISegmentsWorker> _segmentsWorker;
        private readonly Mock<ISplitParser> _featureFlagParser;
        private readonly INotificationProcessor _notificationPorcessor;

        public NotificationPorcessorTests()
        {
            _featureFlagCache = new Mock<ISplitCache>();
            _splitsWorker = new Mock<ISplitsWorker>();
            _segmentsWorker = new Mock<ISegmentsWorker>();
            _featureFlagParser = new Mock<ISplitParser>();

            _notificationPorcessor = new NotificationProcessor(_splitsWorker.Object, _segmentsWorker.Object, _featureFlagCache.Object, _featureFlagParser.Object);
        }

        [TestMethod]
        public void Proccess_SplitUpdate_AddToQueueInWorker()
        {
            // Arrange.
            var notification = new SplitChangeNotification
            {
                Type = NotificationType.SPLIT_UPDATE,
                ChangeNumber = 1585867723838
            };

            // Act.
            _notificationPorcessor.Proccess(notification);

            // Assert.
            _splitsWorker.Verify(mock => mock.AddToQueue(notification.ChangeNumber), Times.Once);
        }

        [TestMethod]
        public void Proccess_SplitKill_AddToQueueInWorker()
        {
            // Arrange.
            var notification = new SplitKillNotification
            {
                Type = NotificationType.SPLIT_KILL,
                ChangeNumber = 1585867723838,
                SplitName = "split-test",
                DefaultTreatment = "off"
            };

            // Act.
            _notificationPorcessor.Proccess(notification);

            // Assert.
            _featureFlagCache.Verify(mock => mock.Kill(notification.ChangeNumber, notification.SplitName, notification.DefaultTreatment), Times.Once);
            _splitsWorker.Verify(mock => mock.AddToQueue(notification.ChangeNumber), Times.Once);
        }

        [TestMethod]
        public void Proccess_SegmentUpdate_AddToQueueInWorker()
        {
            // Arrange.
            var notification = new SegmentChangeNotification
            {
                Type = NotificationType.SEGMENT_UPDATE,
                ChangeNumber = 1585867723838,
                SegmentName = "segment-test"
            };

            // Act.
            _notificationPorcessor.Proccess(notification);

            // Assert.
            _segmentsWorker.Verify(mock => mock.AddToQueue(notification.ChangeNumber, notification.SegmentName), Times.Once);
        }

        [TestMethod]
        public void Process_SplitUpdate_ShouldAddOrUpdate()
        {
            // Arrange.
            var notification = new SplitChangeNotification
            {
                Type = NotificationType.SPLIT_UPDATE,
                ChangeNumber = 1585867723838,
                PreviousChangeNumber = 10,
                CompressionType = CompressionType.Gzip,
                FeatureFlag = new Split
                {
                    name = "test",
                    defaultTreatment = "off",
                    status = "ACTIVE"
                }
            };

            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(10);

            // Act.
            _notificationPorcessor.Proccess(notification);

            // Assert.
            _featureFlagCache.Verify(mock => mock.AddOrUpdate(notification.FeatureFlag.name, It.IsAny<ParsedSplit>()), Times.Once);
            _splitsWorker.Verify(mock => mock.AddToQueue(It.IsAny<long>()), Times.Never);
        }

        [TestMethod]
        public void Process_SplitUpdate_ShouldAddToWorkerQueue()
        {
            // Arrange.
            var notification = new SplitChangeNotification
            {
                Type = NotificationType.SPLIT_UPDATE,
                ChangeNumber = 1585867723838,
                PreviousChangeNumber = 10,
                CompressionType = CompressionType.Gzip,
                FeatureFlag = new Split
                {
                    name = "test",
                    defaultTreatment = "off",
                    status = "ACTIVE"
                }
            };

            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(5);

            // Act.
            _notificationPorcessor.Proccess(notification);

            // Assert.
            _featureFlagCache.Verify(mock => mock.AddOrUpdate(It.IsAny<string>(), It.IsAny<SplitBase>()), Times.Never);
            _splitsWorker.Verify(mock => mock.AddToQueue(notification.ChangeNumber), Times.Once);
        }

        [TestMethod]
        public void Process_SplitUpdate_Donothing()
        {
            // Arrange.
            var notification = new SplitChangeNotification
            {
                Type = NotificationType.SPLIT_UPDATE,
                ChangeNumber = 1585867723838,
                PreviousChangeNumber = 10,
                CompressionType = CompressionType.Gzip,
                FeatureFlag = new Split
                {
                    name = "test",
                    defaultTreatment = "off",
                    status = "ACTIVE"
                }
            };

            _featureFlagCache
                .Setup(mock => mock.GetChangeNumber())
                .Returns(1585867723840);

            // Act.
            _notificationPorcessor.Proccess(notification);

            // Assert.
            _featureFlagCache.Verify(mock => mock.AddOrUpdate(It.IsAny<string>(), It.IsAny<SplitBase>()), Times.Never);
            _splitsWorker.Verify(mock => mock.AddToQueue(It.IsAny<long>()), Times.Never);
        }
    }
}
