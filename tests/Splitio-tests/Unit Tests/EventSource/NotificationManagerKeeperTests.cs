using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.EventSource;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Storages;

namespace Splitio_Tests.Unit_Tests.EventSource
{
    [TestClass]
    public class NotificationManagerKeeperTests
    {
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly SplitQueue<StreamingStatus> _streamingStatusQueue;

        private readonly INotificationManagerKeeper _notificationManagerKeeper;

        public NotificationManagerKeeperTests()
        {
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _streamingStatusQueue = new SplitQueue<StreamingStatus>();

            _notificationManagerKeeper = new NotificationManagerKeeper(_telemetryRuntimeProducer.Object, _streamingStatusQueue);
        }

        [TestMethod]
        public void HandleIncominEvent_ControlStreamingPaused_ShouldDispatchEvent()
        {
            // Arrange.
            var notification = new ControlNotification
            {
                 Channel = "control_pri",
                 ControlType = ControlType.STREAMING_PAUSED,
                 Type = NotificationType.CONTROL
            };

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notification);

            // Assert.
            _streamingStatusQueue.TryDequeue(out StreamingStatus action);
            Assert.AreEqual(StreamingStatus.STREAMING_DOWN, action);
        }

        [TestMethod]
        public void HandleIncominEvent_ControlStreamingResumed_ShouldDispatchEvent()
        {
            // Arrange.
            var notification = new ControlNotification
            {
                Channel = "control_pri",
                ControlType = ControlType.STREAMING_RESUMED,
                Type = NotificationType.CONTROL
            };

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notification);

            // Assert.
            _streamingStatusQueue.TryDequeue(out StreamingStatus action);
            Assert.AreEqual(StreamingStatus.STREAMING_READY, action);
        }

        [TestMethod]
        public void HandleIncominEvent_ControlStreamingResumed_ShouldNotDispatchEvent()
        {
            // Arrange.
            var occupancyNotiSec = new OccupancyNotification
            {
                Channel = "control_sec",
                Metrics = new OccupancyMetricsData { Publishers = 0 },
                Type = NotificationType.OCCUPANCY
            };

            var occupancyNotiPri = new OccupancyNotification
            {
                Channel = "control_pri",
                Metrics = new OccupancyMetricsData { Publishers = 0 },
                Type = NotificationType.OCCUPANCY
            };

            var notification = new ControlNotification
            {
                Channel = "control_pri",
                ControlType = ControlType.STREAMING_RESUMED,
                Type = NotificationType.CONTROL
            };

            // Act & Assert.
            _notificationManagerKeeper.HandleIncomingEvent(occupancyNotiSec);
            Assert.AreEqual(0, _streamingStatusQueue.Count());

            _notificationManagerKeeper.HandleIncomingEvent(occupancyNotiPri);
            _streamingStatusQueue.TryDequeue(out StreamingStatus action);
            Assert.AreEqual(StreamingStatus.STREAMING_DOWN, action);

            _notificationManagerKeeper.HandleIncomingEvent(notification);
            Assert.AreEqual(0, _streamingStatusQueue.Count());
        }

        [TestMethod]
        public void HandleIncominEvent_ControlStreamingDisabled_ShouldDispatchEvent()
        {
            // Arrange.
            var notification = new ControlNotification
            {
                Channel = "control_pri",
                ControlType = ControlType.STREAMING_DISABLED,
                Type = NotificationType.CONTROL
            };

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notification);

            // Assert.
            _streamingStatusQueue.TryDequeue(out StreamingStatus action);
            Assert.AreEqual(StreamingStatus.STREAMING_OFF, action);
        }

        [TestMethod]
        public void HandleIncominEvent_OccupancyWithPublishers_FirstTime_ShouldNotDispatchEvent()
        {
            // Arrange.
            var notification = new OccupancyNotification
            {
                Channel = "control_pri",
                Type = NotificationType.OCCUPANCY,
                Metrics = new OccupancyMetricsData { Publishers = 2 }
            };

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notification);

            // Assert.
            Assert.AreEqual(0, _streamingStatusQueue.Count());
        }

        [TestMethod]
        public void HandleIncominEvent_OccupancyWithPublishers_ManyEvents_MultiReg()
        {
            // Arrange.
            var notificationPri = new OccupancyNotification
            {
                Channel = "control_pri",
                Type = NotificationType.OCCUPANCY,
                Metrics = new OccupancyMetricsData { Publishers = 2 }
            };

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationPri);

            // Assert.
            Assert.AreEqual(0, _streamingStatusQueue.Count());

            // Event control_pri with 0 publishers - should return false
            // Arrange.
            notificationPri.Metrics.Publishers = 0;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(new OccupancyNotification
            {
                Channel = "control_sec",
                Type = NotificationType.OCCUPANCY,
                Metrics = new OccupancyMetricsData { Publishers = 0 }
            });
            _notificationManagerKeeper.HandleIncomingEvent(notificationPri);

            // Assert.
            _streamingStatusQueue.TryDequeue(out StreamingStatus action);
            Assert.AreEqual(StreamingStatus.STREAMING_DOWN, action);

            // Event control_sec with 2 publishers - should return true
            // Arrange.
            var notificationSec = new OccupancyNotification
            {
                Channel = "control_sec",
                Type = NotificationType.OCCUPANCY,
                Metrics = new OccupancyMetricsData { Publishers = 2 }
            };

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationSec);

            // Assert.
            _streamingStatusQueue.TryDequeue(out StreamingStatus action2);
            Assert.AreEqual(StreamingStatus.STREAMING_READY, action2);

            // Event control_pri with 2 publishers - should return null
            // Arrange.
            notificationPri.Metrics.Publishers = 2;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationPri);

            // Assert.
            Assert.AreEqual(0, _streamingStatusQueue.Count());

            // Event control_pri with 0 publishers - should return null
            // Arrange.
            notificationPri.Metrics.Publishers = 0;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationPri);

            // Assert.
            Assert.AreEqual(0, _streamingStatusQueue.Count());

            // Event control_sec with 0 publishers - should return false
            // Arrange.
            notificationSec.Metrics.Publishers = 0;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationSec);

            // Assert.
            _streamingStatusQueue.TryDequeue(out StreamingStatus action3);
            Assert.AreEqual(StreamingStatus.STREAMING_DOWN, action3);

            // Event control_sec with 0 publishers - should return null
            // Arrange.
            notificationSec.Metrics.Publishers = 0;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationSec);

            // Assert.
            Assert.AreEqual(0, _streamingStatusQueue.Count());

            // Event control_sec with 1 publishers - should return true
            // Arrange.
            notificationSec.Metrics.Publishers = 1;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationSec);

            // Assert.
            _streamingStatusQueue.TryDequeue(out StreamingStatus action4);
            Assert.AreEqual(StreamingStatus.STREAMING_READY, action4);
        }
    }
}
