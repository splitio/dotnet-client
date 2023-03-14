﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.EventSource;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;

namespace Splitio_Tests.Unit_Tests.EventSource
{
    [TestClass]
    public class NotificationManagerKeeperTests
    {
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly BlockingCollection<SSEClientActions> _sseClientStatus;

        private readonly INotificationManagerKeeper _notificationManagerKeeper;

        public NotificationManagerKeeperTests()
        {
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _sseClientStatus = new BlockingCollection<SSEClientActions>();

            _notificationManagerKeeper = new NotificationManagerKeeper(_telemetryRuntimeProducer.Object, _sseClientStatus);
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
            _sseClientStatus.TryTake(out SSEClientActions action, 1000);
            Assert.AreEqual(SSEClientActions.SUBSYSTEM_DOWN, action);
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
            _sseClientStatus.TryTake(out SSEClientActions action, 1000);
            Assert.AreEqual(SSEClientActions.SUBSYSTEM_READY, action);
        }

        [TestMethod]
        public void HandleIncominEvent_ControlStreamingResumed_ShouldNotDispatchEvent()
        {
            // Arrange.
            var occupancyNoti = new OccupancyNotification
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
            _notificationManagerKeeper.HandleIncomingEvent(occupancyNoti);
            _sseClientStatus.TryTake(out SSEClientActions action, 1000);
            Assert.AreEqual(SSEClientActions.SUBSYSTEM_DOWN, action);

            _notificationManagerKeeper.HandleIncomingEvent(notification);
            Assert.AreEqual(0, _sseClientStatus.Count);
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
            _sseClientStatus.TryTake(out SSEClientActions action, 1000);
            Assert.AreEqual(SSEClientActions.SUBSYSTEM_OFF, action);
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
            Assert.AreEqual(0, _sseClientStatus.Count);
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
            Assert.AreEqual(0, _sseClientStatus.Count);

            // Event control_pri with 0 publishers - should return false
            // Arrange.
            notificationPri.Metrics.Publishers = 0;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationPri);

            // Assert.
            _sseClientStatus.TryTake(out SSEClientActions action, 1000);
            Assert.AreEqual(SSEClientActions.SUBSYSTEM_DOWN, action);

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
            _sseClientStatus.TryTake(out SSEClientActions action2, 1000);
            Assert.AreEqual(SSEClientActions.SUBSYSTEM_READY, action2);

            // Event control_pri with 2 publishers - should return null
            // Arrange.
            notificationPri.Metrics.Publishers = 2;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationPri);

            // Assert.
            Assert.AreEqual(0, _sseClientStatus.Count);

            // Event control_pri with 0 publishers - should return null
            // Arrange.
            notificationPri.Metrics.Publishers = 0;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationPri);

            // Assert.
            Assert.AreEqual(0, _sseClientStatus.Count);

            // Event control_sec with 0 publishers - should return false
            // Arrange.
            notificationSec.Metrics.Publishers = 0;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationSec);

            // Assert.
            _sseClientStatus.TryTake(out SSEClientActions action3, 1000);
            Assert.AreEqual(SSEClientActions.SUBSYSTEM_DOWN, action3);

            // Event control_sec with 0 publishers - should return null
            // Arrange.
            notificationSec.Metrics.Publishers = 0;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationSec);

            // Assert.
            Assert.AreEqual(0, _sseClientStatus.Count);

            // Event control_sec with 1 publishers - should return true
            // Arrange.
            notificationSec.Metrics.Publishers = 1;

            // Act.
            _notificationManagerKeeper.HandleIncomingEvent(notificationSec);

            // Assert.
            _sseClientStatus.TryTake(out SSEClientActions action4, 1000);
            Assert.AreEqual(SSEClientActions.SUBSYSTEM_READY, action4);
        }
    }
}
