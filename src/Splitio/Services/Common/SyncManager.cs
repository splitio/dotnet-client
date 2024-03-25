using Splitio.CommonLibraries;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EventSource;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class SyncManager : ISyncManager, IQueueObserver
    {
        private readonly bool _streamingEnabled;
        private readonly ISynchronizer _synchronizer;
        private readonly IPushManager _pushManager;
        private readonly ISSEHandler _sseHandler;
        private readonly ISplitLogger _log;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly IStatusManager _statusManager;
        private readonly ITasksManager _tasksManager;

        private readonly ITelemetrySyncTask _telemetrySyncTask;
        private readonly CancellationTokenSource _ctsStreaming;
        private readonly IBackOff _backOff;
        private readonly ISplitTask _startupTask;
        private readonly SplitQueue<StreamingStatus> _streamingStatusQueue;

        private long _startSessionMs;

        public SyncManager(bool streamingEnabled,
            ISynchronizer synchronizer,
            IPushManager pushManager,
            ISSEHandler sseHandler,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            IStatusManager statusManager,
            ITasksManager tasksManager,
            ITelemetrySyncTask telemetrySyncTask,
            IBackOff backOff,
            SplitQueue<StreamingStatus> streamingStatusQueue,
            ISplitTask startupTask)
        {
            _streamingEnabled = streamingEnabled;
            _synchronizer = synchronizer;
            _pushManager = pushManager;
            _sseHandler = sseHandler;
            _log = WrapperAdapter.Instance().GetLogger(typeof(Synchronizer));
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _statusManager = statusManager;
            _tasksManager = tasksManager;
            _telemetrySyncTask = telemetrySyncTask;
            _ctsStreaming = new CancellationTokenSource();
            _backOff = backOff;
            _streamingStatusQueue = streamingStatusQueue;
            _streamingStatusQueue.AddObserver(this);
            _startupTask = startupTask;
            _startupTask.SetFunction(StartupLogicAsync);
        }

        #region Public Methods
        public void Start()
        {
            _startSessionMs = CurrentTimeHelper.CurrentTimeMillis();
            _startupTask.Start();
        }

        public void Shutdown()
        {
            try
            {
                var task = GetShutdownTasks();

                _sseHandler.StopWorkers();

                Task.WaitAll(task.ToArray(), Constants.Gral.DestroyTimeount);

                _synchronizer.ClearFetchersCache();
            }
            catch (Exception ex)
            {
                _log.Error($"Somenthing went wrong destroying the SDK.", ex);
            }
        }

        public async Task ShutdownAsync()
        {
            try
            {
                var task = GetShutdownTasks();

                _sseHandler.StopWorkers();

                await Task.WhenAll(task.ToArray());

                _synchronizer.ClearFetchersCache();
            }
            catch (Exception ex)
            {
                _log.Error($"Somenthing went wrong destroying the SDK.", ex);
            }
        }

        public async Task Notify()
        {
            if (!_streamingStatusQueue.TryDequeue(out StreamingStatus status)) return;

            _log.Debug($"Streaming status received: {status}");

            switch (status)
            {
                case StreamingStatus.STREAMING_READY:
                    _backOff.Reset();
                    await _synchronizer.StopPeriodicFetchingAsync();
                    await _synchronizer.SyncAllAsync();
                    _sseHandler.StartWorkers();
                    await _pushManager.ScheduleConnectionResetAsync();
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.StreamingStatus, (int)StreamingStatusEnum.Enabled));

                    _log.Debug("Streaming up and running.");
                    break;
                case StreamingStatus.STREAMING_BACKOFF:
                    var interval = _backOff.GetInterval(true);
                    _log.Info($"Retryable error in streaming subsystem. Switching to polling and retrying in {interval} milliseconds.");
                    _synchronizer.StartPeriodicFetching();
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));
                    _sseHandler.StopWorkers();
                    await _pushManager.StopAsync();
                    await Task.Delay((int)interval);
                    await _pushManager.StartAsync();
                    break;
                case StreamingStatus.STREAMING_DOWN:
                    _log.Info("Streaming service temporarily unavailable, working in polling mode.");
                    _sseHandler.StopWorkers();
                    _synchronizer.StartPeriodicFetching();
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));
                    break;
                case StreamingStatus.STREAMING_OFF:
                    _log.Info("Unrecoverable error in streaming subsystem. SDK will work in polling-mode and will not retry an SSE connection.");
                    await _pushManager.StopAsync();
                    _synchronizer.StartPeriodicFetching();
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));
                    _ctsStreaming.Cancel();
                    _ctsStreaming.Dispose();
                    break;
                default:
                    _log.Info($"OnStreamingStatus: Unrecognized status - {status}");
                    break;
            }
        }
        #endregion

        #region Private Methods
        private async Task StartStreamingModeAsync()
        {
            _log.Debug("Starting streaming mode...");
            await _pushManager.StartAsync();
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Streaming));
        }

        private void StartPollingMode()
        {
            _log.Debug("Starting polling mode ...");
            _synchronizer.StartPeriodicFetching();
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));
        }

        private async Task StartupLogicAsync()
        {
            try
            {
                var clock = new Stopwatch();
                clock.Start();

                while (!await _synchronizer.SyncAllAsync() && !_statusManager.IsDestroyed())
                {
                    await Task.Delay(500);
                }

                if (_statusManager.IsDestroyed()) return;

                _statusManager.SetReady();
                clock.Stop();
                _log.Debug($"Time until SDK ready: {clock.ElapsedMilliseconds} ms.");
                _telemetrySyncTask.RecordConfigInit(clock.ElapsedMilliseconds);
                _synchronizer.StartPeriodicDataRecording();

                if (_streamingEnabled)
                    await StartStreamingModeAsync();
                else
                    StartPollingMode();
            }
            catch (Exception ex)
            {
                _log.Debug("Exception initialization SDK.", ex);
            }
        }

        private List<Task> GetShutdownTasks()
        {
            _telemetryRuntimeProducer.RecordSessionLength(CurrentTimeHelper.CurrentTimeMillis() - _startSessionMs);
            _ctsStreaming.Cancel();
            _ctsStreaming.Dispose();

            return new List<Task>
            {
                _synchronizer.StopPeriodicDataRecordingAsync(),
                _synchronizer.StopPeriodicFetchingAsync(),
                _pushManager.StopAsync(),
                _startupTask.StopAsync(),
                _tasksManager.DestroyAsync()
            };
        }
        #endregion
    }
}