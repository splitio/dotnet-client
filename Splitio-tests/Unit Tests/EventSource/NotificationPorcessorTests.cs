using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EventSource;
using Splitio.Services.EventSource.Workers;

namespace Splitio_Tests.Unit_Tests.EventSource
{
    [TestClass]
    public class NotificationPorcessorTests
    {
        private readonly Mock<ISplitCache> _splitCache;
        private readonly Mock<ISplitsWorker> _splitsWorker;
        private readonly Mock<ISegmentsWorker> _segmentsWorker;
        private readonly INotificationProcessor _notificationPorcessor;

        public NotificationPorcessorTests()
        {
            _splitCache = new Mock<ISplitCache>();
            _splitsWorker = new Mock<ISplitsWorker>();
            _segmentsWorker = new Mock<ISegmentsWorker>();

            _notificationPorcessor = new NotificationProcessor(_splitsWorker.Object, _segmentsWorker.Object, _splitCache.Object);
        }

        [TestMethod]
        public void Proccess_SplitUpdate_AddToQueueInWorker()
        {
            // Arrange.
            var notification = new SplitChangeNotifiaction
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
            _splitCache.Verify(mock => mock.Kill(notification.ChangeNumber, notification.SplitName, notification.DefaultTreatment), Times.Once);
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
    }
}
