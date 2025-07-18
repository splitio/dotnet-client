﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.EventSource;
using Splitio.Services.EventSource.Workers;

namespace Splitio_Tests.Unit_Tests.EventSource
{
    [TestClass]
    public class NotificationPorcessorTests
    {
        private readonly Mock<ISplitsWorker> _splitsWorker;
        private readonly Mock<ISegmentsWorker> _segmentsWorker;
        private readonly INotificationProcessor _notificationPorcessor;

        public NotificationPorcessorTests()
        {
            _splitsWorker = new Mock<ISplitsWorker>();
            _segmentsWorker = new Mock<ISegmentsWorker>();

            _notificationPorcessor = new NotificationProcessor(_splitsWorker.Object, _segmentsWorker.Object);
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
            _splitsWorker.Verify(mock => mock.AddToQueue(notification), Times.Once);
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
            _splitsWorker.Verify(mock => mock.Kill(notification), Times.Once);
            _splitsWorker.Verify(mock => mock.AddToQueue(It.IsAny<SplitChangeNotification>()), Times.Once);
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
        public void Process_RuleBasedSegmentUpdate_AddToQueue()
        {
            // Arrange
            var notification = new RuleBasedSegmentNotification
            {
                Type = NotificationType.RB_SEGMENT_UPDATE,
                ChangeNumber = 100,
                Data = "",
                Channel = "FLAGS_UPDATE"
            };

            // Act
            _notificationPorcessor.Proccess(notification);

            // Assert
            _splitsWorker.Verify(mock => mock.AddToQueue(notification), Times.Once);
        }
    }
}
