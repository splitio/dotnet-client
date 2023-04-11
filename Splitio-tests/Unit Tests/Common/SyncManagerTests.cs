using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.EventSource;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
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
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly Mock<IStatusManager> _statusManager;
        private readonly Mock<ITelemetrySyncTask> _telemetrySyncTask;
        private readonly BlockingCollection<SSEClientActions> _sseClientStatus;

        public SyncManagerTests()
        {
            _wrapperAdapter = WrapperAdapter.Instance();
            _taskManager = new TasksManager(_wrapperAdapter);

            _synchronizer = new Mock<ISynchronizer>();
            _pushManager = new Mock<IPushManager>();
            _sseHandler = new Mock<ISSEHandler>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _telemetrySyncTask = new Mock<ITelemetrySyncTask>();
            _statusManager = new Mock<IStatusManager>();
            _sseClientStatus = new BlockingCollection<SSEClientActions>(new ConcurrentQueue<SSEClientActions>());
        }

        [TestMethod]
        public void Start_WithStreamingDisabled_ShouldStartPoll()
        {
            // Arrange.
            var streamingEnabled = false;
            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            var syncManager = GetSyncManager(streamingEnabled);

            // Act.
            syncManager.Start();

            // Assert.
            Thread.Sleep(1000);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicDataRecording(), Times.Once);
            _pushManager.Verify(mock => mock.StartSseAsync(), Times.Never);
        }

        [TestMethod]
        public void Start_WithStreamingEnabled_ShouldStartStream()
        {
            // Arrange.
            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            _pushManager
                .Setup(mock => mock.StartSseAsync())
                .ReturnsAsync(true);

            var streamingEnabled = true;
            var syncManager = GetSyncManager(streamingEnabled);

            // Act.
            syncManager.Start();

            // Assert.            
            Thread.Sleep(1000);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);
            _pushManager.Verify(mock => mock.StartSseAsync(), Times.Once);
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
                .Setup(mock => mock.StartSseAsync())
                .ReturnsAsync(false);

            var streamingEnabled = true;
            var syncManager = GetSyncManager(streamingEnabled);

            // Act.
            syncManager.Start();

            // Assert.            
            Thread.Sleep(1000);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);
            _pushManager.Verify(mock => mock.StartSseAsync(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicDataRecording(), Times.Once);
        }

        [TestMethod]
        public void Shutdown()
        {
            // Arrange.
            var streamingEnabled = true;
            var syncManager = GetSyncManager(streamingEnabled);

            // Act.
            syncManager.Shutdown();

            // Assert.
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);
            _synchronizer.Verify(mock => mock.StopPeriodicDataRecording(), Times.Once);
            _pushManager.Verify(mock => mock.StopSse(), Times.Once);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_Connected()
        {
            // Arrange.
            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            _pushManager
                .Setup(mock => mock.StartSseAsync())
                .ReturnsAsync(true);

            var streamingEnabled = true;
            var syncManager = GetSyncManager(streamingEnabled);
            syncManager.Start();
            Thread.Sleep(1000);

            // Act & Assert.
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);

            _sseClientStatus.Add(SSEClientActions.CONNECTED);
            Thread.Sleep(50);

            _sseHandler.Verify(mock => mock.StartWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);

            // Act & Assert.
            _sseClientStatus.Add(SSEClientActions.CONNECTED);
            Thread.Sleep(50);

            _sseHandler.Verify(mock => mock.StartWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_Disconnect()
        {
            // Arrange.
            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            _pushManager
                .Setup(mock => mock.StartSseAsync())
                .ReturnsAsync(true);

            var streamingEnabled = true;
            var syncManager = GetSyncManager(streamingEnabled);
            syncManager.Start();
            Thread.Sleep(1000);

            // Act & Assert.
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);
            _pushManager.Verify(mock => mock.StartSseAsync(), Times.Once);

            _sseClientStatus.Add(SSEClientActions.DISCONNECT);
            Thread.Sleep(50);

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Never);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Never);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), true), Times.Never);

            // Act & Assert.
            _sseClientStatus.Add(SSEClientActions.CONNECTED);
            Thread.Sleep(50);

            _sseHandler.Verify(mock => mock.StartWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);
            _pushManager.Verify(mock => mock.StartSseAsync(), Times.Once);

            _sseClientStatus.Add(SSEClientActions.DISCONNECT);
            Thread.Sleep(50);

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), true), Times.Exactly(2));
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _pushManager.Verify(mock => mock.StartSseAsync(), Times.Once);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_RetryableError()
        {
            // Arrange.
            var streamingEnabled = true;
            
            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            _pushManager
                .Setup(mock => mock.StartSseAsync())
                .ReturnsAsync(false);

            var syncManager = GetSyncManager(streamingEnabled);
            syncManager.Start();
            Thread.Sleep(1000);

            // Act & Assert.
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);

            _sseClientStatus.Add(SSEClientActions.RETRYABLE_ERROR);
            Thread.Sleep(50);

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Never);
            
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), true), Times.Never);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _pushManager.Verify(mock => mock.StartSseAsync(), Times.Once);

            // Act & Assert.
            _sseClientStatus.Add(SSEClientActions.CONNECTED);
            Thread.Sleep(50);
            _sseHandler.Verify(mock => mock.StartWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), true), Times.Once);
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);

            _sseClientStatus.Add(SSEClientActions.RETRYABLE_ERROR);
            Thread.Sleep(50);

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), true), Times.Exactly(2));
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Exactly(2));
            _pushManager.Verify(mock => mock.StartSseAsync(), Times.Exactly(2));
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_NonRetryableError()
        {
            // Arrange.
            var streamingEnabled = true;
            
            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            _pushManager
                .Setup(mock => mock.StartSseAsync())
                .ReturnsAsync(false);

            var syncManager = GetSyncManager(streamingEnabled);
            syncManager.Start();
            Thread.Sleep(1000);

            // Act & Assert.
            _sseClientStatus.Add(SSEClientActions.NONRETRYABLE_ERROR);
            Thread.Sleep(50);

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Never);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), true), Times.Never);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _pushManager.Verify(mock => mock.StartSseAsync(), Times.Once);

            // Act & Assert.
            _sseClientStatus.Add(SSEClientActions.CONNECTED);
            Thread.Sleep(50);
            _sseHandler.Verify(mock => mock.StartWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), true), Times.Once);
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);

            _sseClientStatus.Add(SSEClientActions.NONRETRYABLE_ERROR);
            Thread.Sleep(50);

            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), true), Times.Exactly(2));
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Exactly(2));
            _pushManager.Verify(mock => mock.StartSseAsync(), Times.Once);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_SubsystemDown()
        {
            // Arrange.
            var streamingEnabled = true;

            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            _pushManager
                .Setup(mock => mock.StartSseAsync())
                .ReturnsAsync(true);

            var syncManager = GetSyncManager(streamingEnabled);
            syncManager.Start();
            Thread.Sleep(1000);

            // Act.
            _sseClientStatus.Add(SSEClientActions.SUBSYSTEM_DOWN);
            Thread.Sleep(50);

            // Assert.
            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
        }


        [TestMethod]
        public void OnProcessFeedbackSSE_SubsystemReady()
        {
            // Arrange.
            var streamingEnabled = true;

            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            _pushManager
                .Setup(mock => mock.StartSseAsync())
                .ReturnsAsync(true);

            var syncManager = GetSyncManager(streamingEnabled);
            syncManager.Start();
            Thread.Sleep(1000);

            // Act.
            _sseClientStatus.Add(SSEClientActions.SUBSYSTEM_READY);
            Thread.Sleep(50);

            // Assert.
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), true), Times.Once);
            _sseHandler.Verify(mock => mock.StartWorkers(), Times.Once);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_SubsystemOff()
        {
            // Arrange.
            var streamingEnabled = true;

            _synchronizer
                .Setup(mock => mock.SyncAll(It.IsAny<CancellationTokenSource>(), false))
                .Returns(true);

            _pushManager
                .Setup(mock => mock.StartSseAsync())
                .ReturnsAsync(true);

            var syncManager = GetSyncManager(streamingEnabled);
            syncManager.Start();
            Thread.Sleep(1000);

            // Act.
            _sseClientStatus.Add(SSEClientActions.SUBSYSTEM_OFF);
            Thread.Sleep(50);

            // Assert.
            _pushManager.Verify(mock => mock.StopSse(), Times.Once);
        }

        private ISyncManager GetSyncManager(bool streamingEnabled)
        {
            return new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _telemetryRuntimeProducer.Object,
                _statusManager.Object,
                _taskManager,
                _wrapperAdapter,
                _telemetrySyncTask.Object,
                _sseClientStatus);
        }
    }
}
