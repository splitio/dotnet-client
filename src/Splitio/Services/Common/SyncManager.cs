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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class SyncManager : ISyncManager
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
        private readonly BlockingCollection<StreamingStatus> _streamingStatusQueue;
        private readonly CancellationTokenSource _ctsShutdown;
        private readonly IBackOff _backOff;
        private readonly ISplitTask _startupTask;
        private readonly ISplitTask _onStreamingStatusTask;

        public SyncManager(bool streamingEnabled,
            ISynchronizer synchronizer,
            IPushManager pushManager,
            ISSEHandler sseHandler,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            IStatusManager statusManager,
            ITasksManager tasksManager,
            ITelemetrySyncTask telemetrySyncTask,
            BlockingCollection<StreamingStatus> streamingStatusQueue,
            IBackOff backOff,
            ISplitTask startupTask,
            ISplitTask onStreamingStatusTask)
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
            _streamingStatusQueue = streamingStatusQueue;
            _ctsStreaming = new CancellationTokenSource();
            _ctsShutdown = new CancellationTokenSource();
            _backOff = backOff;
            _startupTask = startupTask;
            _startupTask.SetAction(async () => await StartupLogic());
            _onStreamingStatusTask = onStreamingStatusTask;
            _onStreamingStatusTask.SetAction(OnStreamingStatus);
        }

        #region Public Methods
        public void Start()
        {
            _startupTask.Start();
        }

        public async Task ShutdownAsync()
        {
            _log.Info("Initialitation sdk destroy.");

            await _startupTask.StopAsync();
            await _onStreamingStatusTask.StopAsync();

            _ctsStreaming.Cancel();
            _ctsStreaming.Dispose();

            _pushManager.Stop();
            _synchronizer.StopPeriodicFetching();
            _synchronizer.ClearFetchersCache();
            _synchronizer.StopPeriodicDataRecording();

            _ctsShutdown.Cancel();
            _ctsShutdown.Dispose();

            await _tasksManager.DestroyAsync();

            _log.Info("SDK has been destroyed.");
        }

        public void OnStreamingStatus()
        {
            try
            {
                while (!_ctsStreaming.IsCancellationRequested)
                {
                    if (_streamingStatusQueue.TryTake(out StreamingStatus status, -1, _ctsStreaming.Token))
                    {
                        _log.Debug($"Streaming status received: {status}");

                        switch (status)
                        {
                            case StreamingStatus.STREAMING_READY:
                                _backOff.Reset();
                                _synchronizer.StopPeriodicFetching();
                                _synchronizer.SyncAll(_ctsShutdown);
                                _sseHandler.StartWorkers();
                                _pushManager.ScheduleConnectionReset();
                                _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.StreamingStatus, (int)StreamingStatusEnum.Enabled));

                                _log.Debug("Streaming up and running.");
                                break;
                            case StreamingStatus.STREAMING_BACKOFF:
                                var interval = _backOff.GetInterval(true);
                                _log.Info($"Retryable error in streaming subsystem. Switching to polling and retrying in {interval} milliseconds.");
                                _synchronizer.StartPeriodicFetching();
                                _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));
                                _sseHandler.StopWorkers();
                                _pushManager.Stop();
                                Task.Delay((int)interval).Wait();
                                _pushManager.Start();
                                break;
                            case StreamingStatus.STREAMING_DOWN:
                                _log.Info("Streaming service temporarily unavailable, working in polling mode.");
                                _sseHandler.StopWorkers();
                                _synchronizer.StartPeriodicFetching();
                                _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));
                                break;
                            case StreamingStatus.STREAMING_OFF:
                                _log.Info("Unrecoverable error in streaming subsystem. SDK will work in polling-mode and will not retry an SSE connection.");
                                _pushManager.Stop();
                                _synchronizer.StartPeriodicFetching();
                                _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));
                                _ctsStreaming.Cancel();
                                break;
                            default:
                                _log.Info($"OnStreamingStatus: Unrecognized status - {status}");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_ctsStreaming.IsCancellationRequested)
                    _log.Debug("OnStreamingStatus Exception", ex);
            }
            finally
            { _streamingStatusQueue.Dispose(); }
        }
        #endregion

        #region Private Methods
        private async Task StartStreamingModeAsync()
        {
            _log.Debug("Starting streaming mode...");
            _onStreamingStatusTask.Start();
            await _pushManager.Start();
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Streaming));
        }

        private void StartPollingMode()
        {
            _log.Debug("Starting polling mode ...");
            _synchronizer.StartPeriodicFetching();
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));
        }

        private async Task StartupLogic()
        {
            try
            {
                Console.WriteLine($"### {Thread.CurrentThread.ManagedThreadId} StartupLogic Starting ....");
                while (!await _synchronizer.SyncAll(_ctsShutdown, asynchronous: false))
                {
                    await Task.Delay(500);
                }

                if (_statusManager.IsDestroyed()) return;

                _statusManager.SetReady();
                // TODO: calculate time to be ready
                _telemetrySyncTask.RecordConfigInit(1000);
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
            finally
            {
                Console.WriteLine($"### {Thread.CurrentThread.ManagedThreadId} StartupLogic finally");
            }
        }
        #endregion
    }
}
