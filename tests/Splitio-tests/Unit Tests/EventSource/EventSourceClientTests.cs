using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.EventSource;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Storages;
using System;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.EventSource
{
    [TestClass]
    public class EventSourceClientTests
    {
        private readonly Mock<INotificationProcessor> _notificationPorcessor;
        private readonly Mock<INotificationParser> _notificationParser;
        private readonly Mock<INotificationManagerKeeper> _notificationManagerKeeper;
        private readonly Mock<ISplitioHttpClient> _httpClient;
        private readonly EventSourceClient _eventSourceClient;
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryProducer;
        private readonly Mock<IStatusManager> _statusManager;
        private readonly Mock<ISplitTask> _splitTask;
        private readonly TasksManager _tasksManager;

        public EventSourceClientTests()
        {
            _notificationManagerKeeper = new Mock<INotificationManagerKeeper>();
            _notificationParser = new Mock<INotificationParser>();
            _httpClient = new Mock<ISplitioHttpClient>();
            _telemetryProducer = new Mock<ITelemetryRuntimeProducer>();
            _statusManager = new Mock<IStatusManager>();
            _tasksManager = new TasksManager(_statusManager.Object);

            var connectTask = _tasksManager.NewOnTimeTask(Splitio.Enums.Task.SSEConnect);

            _eventSourceClient = new EventSourceClient(_notificationParser.Object,
                _httpClient.Object, _telemetryProducer.Object, 
                _notificationManagerKeeper.Object, _statusManager.Object, connectTask);
        }

        [TestMethod]
        public void ConectFail_ShouldRetry()
        {
            // Arrange
            _statusManager
                .Setup(manager => manager.IsDestroyed())
                .Returns(false);

            // Act.
            _eventSourceClient.Connect("https://dummy");
            Thread.Sleep(1000);

            // Assert.
            _notificationManagerKeeper.Verify(mock => mock.HandleSseStatus(SSEClientStatusMessage.INITIALIZATION_IN_PROGRESS), Times.Once);
            _notificationManagerKeeper.Verify(mock => mock.HandleSseStatus(SSEClientStatusMessage.RETRYABLE_ERROR), Times.Once);
        }
    }
}
