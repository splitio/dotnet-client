using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.EventSource;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Storages;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class SyncManagerTests
    {
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly ITasksManager _taskManager;
        private readonly Mock<ISynchronizer> _synchronizer;
        private readonly Mock<IPushManager> _pushManager;
        private readonly Mock<ISSEHandler> _sseHandler;
        private readonly Mock<ISplitLogger> _log;
        private readonly Mock<INotificationManagerKeeper> _notificationManagerKeeper;
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly Mock<IReadinessGatesCache> _gates;
        private readonly Mock<ITelemetrySyncTask> _telemetrySyncTask;
        private ISyncManager _syncManager;

        public SyncManagerTests()
        {
            _wrapperAdapter = new WrapperAdapter();
            _taskManager = new TasksManager(_wrapperAdapter);

            _synchronizer = new Mock<ISynchronizer>();
            _pushManager = new Mock<IPushManager>();
            _sseHandler = new Mock<ISSEHandler>();
            _log = new Mock<ISplitLogger>();
            _notificationManagerKeeper = new Mock<INotificationManagerKeeper>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _telemetrySyncTask = new Mock<ITelemetrySyncTask>();
            _gates = new Mock<IReadinessGatesCache>();
        }

        [TestMethod]
        public void Start_WithStreamingDisabled_ShouldStartPoll()
        {
            // Arrange.
            var streamingEnabled = false;
            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            _syncManager = new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _notificationManagerKeeper.Object,
                _telemetryRuntimeProducer.Object,
                _gates.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _log.Object);

            // Act.
            _syncManager.Start();

            // Assert.
            Thread.Sleep(500);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicDataRecording(), Times.Once);
            _pushManager.Verify(mock => mock.StartSse(), Times.Never);
        }

        [TestMethod]
        public void Start_WithStreamingEnabled_ShouldStartStream()
        {
            // Arrange.
            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            _pushManager
                .Setup(mock => mock.StartSse())
                .ReturnsAsync(true);

            var streamingEnabled = true;
            _syncManager = new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _notificationManagerKeeper.Object,
                _telemetryRuntimeProducer.Object,
                _gates.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _log.Object);

            // Act.
            _syncManager.Start();

            // Assert.            
            Thread.Sleep(500);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);
            _pushManager.Verify(mock => mock.StartSse(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Never);
            _synchronizer.Verify(mock => mock.StartPeriodicDataRecording(), Times.Once);
        }

        [TestMethod]
        public void Start_WithStreamingEnabled_ShouldStartPolling()
        {
            // Arrange.
            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            _pushManager
                .Setup(mock => mock.StartSse())
                .ReturnsAsync(false);            

            var streamingEnabled = true;
            _syncManager = new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _notificationManagerKeeper.Object,
                _telemetryRuntimeProducer.Object,
                _gates.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _log.Object);

            // Act.
            _syncManager.Start();

            // Assert.            
            Thread.Sleep(500);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);            
            _pushManager.Verify(mock => mock.StartSse(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicDataRecording(), Times.Once);
        }

        [TestMethod]
        public void Shutdown()
        {
            // Arrange.
            var streamingEnabled = true;
            _syncManager = new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _notificationManagerKeeper.Object,
                _telemetryRuntimeProducer.Object,
                _gates.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _log.Object);

            // Act.
            _syncManager.Shutdown();

            // Assert.
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);
            _synchronizer.Verify(mock => mock.StopPeriodicDataRecording(), Times.Once);
            _pushManager.Verify(mock => mock.StopSse(), Times.Once);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_Connected()
        {
            // Arrange.
            var streamingEnabled = true;
            var syncManager = new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _notificationManagerKeeper.Object,
                _telemetryRuntimeProducer.Object,
                _gates.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _log.Object);

            // Act & Assert.
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.CONNECTED));

            _sseHandler.Verify(mock => mock.StartWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Once);
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);

            // Act & Assert.
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.CONNECTED));

            _sseHandler.Verify(mock => mock.StartWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Once);
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_Disconnect()
        {
            // Arrange.
            var streamingEnabled = true;
            var syncManager = new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _notificationManagerKeeper.Object,
                _telemetryRuntimeProducer.Object,
                _gates.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _log.Object);

            // Act & Assert.
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.DISCONNECT));

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Never);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Never);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Never);
            _pushManager.Verify(mock => mock.StartSse(), Times.Never);

            // Act & Assert.
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.CONNECTED));
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.DISCONNECT));

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Exactly(2));
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _pushManager.Verify(mock => mock.StartSse(), Times.Never);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_RetryableError()
        {
            // Arrange.
            var streamingEnabled = true;
            var syncManager = new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _notificationManagerKeeper.Object,
                _telemetryRuntimeProducer.Object,
                _gates.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _log.Object);

            // Act & Assert.
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.RETRYABLE_ERROR));

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Never);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Never);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Never);
            _pushManager.Verify(mock => mock.StartSse(), Times.Never);

            // Act & Assert.
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.CONNECTED));
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.RETRYABLE_ERROR));

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Exactly(2));
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _pushManager.Verify(mock => mock.StartSse(), Times.Once);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_NonRetryableError()
        {
            // Arrange.
            var streamingEnabled = true;
            var syncManager = new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _notificationManagerKeeper.Object,
                _telemetryRuntimeProducer.Object,
                _gates.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _log.Object);

            // Act & Assert.
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.NONRETRYABLE_ERROR));

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Never);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Never);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Never);
            _pushManager.Verify(mock => mock.StartSse(), Times.Never);

            // Act & Assert.
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.CONNECTED));
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.NONRETRYABLE_ERROR));

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Exactly(2));
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _pushManager.Verify(mock => mock.StartSse(), Times.Never);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_SubsystemDown()
        {
            // Arrange.
            var streamingEnabled = true;
            var syncManager = new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _notificationManagerKeeper.Object,
                _telemetryRuntimeProducer.Object,
                _gates.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _log.Object);

            // Act.
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.SUBSYSTEM_DOWN));

            // Assert.
            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
        }


        [TestMethod]
        public void OnProcessFeedbackSSE_SubsystemReady()
        {
            // Arrange.
            var streamingEnabled = true;
            var syncManager = new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _notificationManagerKeeper.Object,
                _telemetryRuntimeProducer.Object,
                _gates.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _log.Object);

            // Act.
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.SUBSYSTEM_READY));

            // Assert.
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Once);
            _sseHandler.Verify(mock => mock.StartWorkers(), Times.Once);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_SubsystemOff()
        {
            // Arrange.
            var streamingEnabled = true;
            var syncManager = new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _notificationManagerKeeper.Object,
                _telemetryRuntimeProducer.Object,
                _gates.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _log.Object);

            // Act.
            syncManager.OnProcessFeedbackSSE(this, new SSEActionsEventArgs(SSEClientActions.SUBSYSTEM_OFF));

            // Assert.
            _pushManager.Verify(mock => mock.StopSse(), Times.Once);
        }
    }
}
