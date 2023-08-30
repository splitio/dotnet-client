using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.EventSource;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class SyncManagerTests
    {
        private readonly ITasksManager _taskManager;
        private readonly Mock<ISynchronizer> _synchronizer;
        private readonly Mock<IPushManager> _pushManager;
        private readonly Mock<ISSEHandler> _sseHandler;
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly Mock<IStatusManager> _statusManager;
        private readonly Mock<ITelemetrySyncTask> _telemetrySyncTask;
        private readonly Mock<IBackOff> _backoff;

        private readonly BlockingCollection<StreamingStatus> _streamingStatusQueue;

        public SyncManagerTests()
        {
            _statusManager = new Mock<IStatusManager>();
            _taskManager = new TasksManager(_statusManager.Object);
            _synchronizer = new Mock<ISynchronizer>();
            _pushManager = new Mock<IPushManager>();
            _sseHandler = new Mock<ISSEHandler>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _telemetrySyncTask = new Mock<ITelemetrySyncTask>();
            _backoff = new Mock<IBackOff>();
            _streamingStatusQueue = new BlockingCollection<StreamingStatus>(new ConcurrentQueue<StreamingStatus>());
        }

        [TestMethod]
        public void Start_WithStreamingDisabled_ShouldStartPoll()
        {
            // Arrange.
            var streamingEnabled = false;
            _synchronizer
                .Setup(mock => mock.SyncAllAsync())
                .ReturnsAsync(true);

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(false);

            var syncManager = GetSyncManager(streamingEnabled);

            // Act.
            syncManager.Start();

            // Assert.
            Thread.Sleep(3000);
            _synchronizer.Verify(mock => mock.SyncAllAsync(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicDataRecording(), Times.Once);
            _pushManager.Verify(mock => mock.StartAsync(), Times.Never);
        }

        [TestMethod]
        public void Start_WithStreamingEnabled_ShouldStartStream()
        {
            // Arrange.
            _synchronizer
                .Setup(mock => mock.SyncAllAsync())
                .ReturnsAsync(true);

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(false);

            var streamingEnabled = true;
            var syncManager = GetSyncManager(streamingEnabled);

            // Act.
            syncManager.Start();

            // Assert.            
            Thread.Sleep(3000);
            _synchronizer.Verify(mock => mock.SyncAllAsync(), Times.Once);
            _pushManager.Verify(mock => mock.StartAsync(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Never);
            _synchronizer.Verify(mock => mock.StartPeriodicDataRecording(), Times.Once);
        }

        [TestMethod]
        public void Start_WithStreamingEnabled_ShouldStartPolling()
        {
            // Arrange.
            _synchronizer
                .Setup(mock => mock.SyncAllAsync())
                .ReturnsAsync(true);

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(false);

            var streamingEnabled = true;
            var syncManager = GetSyncManager(streamingEnabled);

            // Act.
            syncManager.Start();

            // Assert.
            Thread.Sleep(3000);
            _synchronizer.Verify(mock => mock.SyncAllAsync(), Times.Once);
            _pushManager.Verify(mock => mock.StartAsync(), Times.Once);
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
            _synchronizer.Verify(mock => mock.ClearFetchersCache(), Times.Once);
            _pushManager.Verify(mock => mock.Stop(), Times.Once);
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_STREAMING_READY()
        {
            // Arrange.
            _synchronizer
                .Setup(mock => mock.SyncAllAsync())
                .ReturnsAsync(true);

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(false);

            var streamingEnabled = true;
            var syncManager = GetSyncManager(streamingEnabled);
            syncManager.Start();
            Thread.Sleep(3000);

            // Act & Assert.
            _synchronizer.Verify(mock => mock.SyncAllAsync(), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordStreamingEvent(It.IsAny<StreamingEvent>()), Times.Exactly(1));
            _pushManager.Verify(mock => mock.StartAsync(), Times.Once);

            _streamingStatusQueue.Add(StreamingStatus.STREAMING_READY);
            Thread.Sleep(3000);

            _sseHandler.Verify(mock => mock.StartWorkers(), Times.Once);
            _synchronizer.Verify(mock => mock.StopPeriodicFetching(), Times.Once);
            _backoff.Verify(mock => mock.Reset(), Times.Once);
            _synchronizer.Verify(mock => mock.SyncAllAsync(), Times.Exactly(2));
            _pushManager.Verify(mock => mock.ScheduleConnectionReset(), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordStreamingEvent(It.IsAny<StreamingEvent>()), Times.Exactly(2));
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_STREAMING_OFF()
        {
            // Arrange.
            _synchronizer
                .Setup(mock => mock.SyncAllAsync())
                .ReturnsAsync(true);

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(false);

            var streamingEnabled = true;
            var syncManager = GetSyncManager(streamingEnabled);
            syncManager.Start();
            Thread.Sleep(3000);

            // Act & Assert.
            _synchronizer.Verify(mock => mock.SyncAllAsync(), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordStreamingEvent(It.IsAny<StreamingEvent>()), Times.Exactly(1));
            _pushManager.Verify(mock => mock.StartAsync(), Times.Once);

            _streamingStatusQueue.Add(StreamingStatus.STREAMING_OFF);
            Thread.Sleep(3000);

            _pushManager.Verify(mock => mock.Stop(), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordStreamingEvent(It.IsAny<StreamingEvent>()), Times.Exactly(2));
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_STREAMING_BACKOFF()
        {
            // Arrange.
            var streamingEnabled = true;
            
            _synchronizer
                .Setup(mock => mock.SyncAllAsync())
                .ReturnsAsync(true);

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(false);

            var syncManager = GetSyncManager(streamingEnabled);
            syncManager.Start();
            Thread.Sleep(3000);

            // Act & Assert.
            _synchronizer.Verify(mock => mock.SyncAllAsync(), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordStreamingEvent(It.IsAny<StreamingEvent>()), Times.Exactly(1));
            _pushManager.Verify(mock => mock.StartAsync(), Times.Once);

            _streamingStatusQueue.Add(StreamingStatus.STREAMING_BACKOFF);
            Thread.Sleep(3000);

            _backoff.Verify(mock => mock.GetInterval(true), Times.Once);
            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordStreamingEvent(It.IsAny<StreamingEvent>()), Times.Exactly(2));
            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Once);
            _pushManager.Verify(mock => mock.Stop(), Times.Once);
            _pushManager.Verify(mock => mock.StartAsync(), Times.Exactly(2));
        }

        [TestMethod]
        public void OnProcessFeedbackSSE_STREAMING_DOWN()
        {
            // Arrange.
            var streamingEnabled = true;
            
            _synchronizer
                .Setup(mock => mock.SyncAllAsync())
                .ReturnsAsync(true);

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(false);

            var syncManager = GetSyncManager(streamingEnabled);
            syncManager.Start();
            Thread.Sleep(2000);

            // Act & Assert.
            _streamingStatusQueue.Add(StreamingStatus.STREAMING_DOWN);
            Thread.Sleep(5000);

            _synchronizer.Verify(mock => mock.StartPeriodicFetching(), Times.Once);
            _sseHandler.Verify(mock => mock.StopWorkers(), Times.Once);
            _telemetryRuntimeProducer.Verify(mock => mock.RecordStreamingEvent(It.IsAny<StreamingEvent>()), Times.Exactly(2));
        }

        private ISyncManager GetSyncManager(bool streamingEnabled)
        {
            var startupTask = _taskManager.NewOnTimeTask(Splitio.Enums.Task.SDKInitialization);
            var streamingStatusTask = _taskManager.NewPeriodicTask(Splitio.Enums.Task.OnStreamingStatusTask, 0);

            return new SyncManager(streamingEnabled,
                _synchronizer.Object,
                _pushManager.Object,
                _sseHandler.Object,
                _telemetryRuntimeProducer.Object,
                _statusManager.Object,
                _taskManager,
                _telemetrySyncTask.Object,
                _streamingStatusQueue,
                _backoff.Object,
                startupTask,
                streamingStatusTask);
        }
    }
}
