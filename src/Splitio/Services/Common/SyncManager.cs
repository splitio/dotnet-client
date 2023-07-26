using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EventSource;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Concurrent;
using System.Threading;

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
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly ITelemetrySyncTask _telemetrySyncTask;
        private readonly CancellationTokenSource _ctsStreaming;
        private readonly BlockingCollection<SSEClientActions> _sseClientStatusQueue;
        private readonly CancellationTokenSource _ctsShutdown;
        private readonly object _lock = new object();

        private bool _streamingConnected;

        public SyncManager(bool streamingEnabled,
            ISynchronizer synchronizer,
            IPushManager pushManager,
            ISSEHandler sseHandler,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            IStatusManager statusManager,
            ITasksManager tasksManager,
            IWrapperAdapter wrapperAdapter,
            ITelemetrySyncTask telemetrySyncTask,
            BlockingCollection<SSEClientActions> sseClientStatus)
        {
            _streamingEnabled = streamingEnabled;
            _synchronizer = synchronizer;
            _pushManager = pushManager;
            _sseHandler = sseHandler;
            _log = WrapperAdapter.Instance().GetLogger(typeof(Synchronizer));
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _statusManager = statusManager;
            _tasksManager = tasksManager;
            _wrapperAdapter = wrapperAdapter;
            _telemetrySyncTask = telemetrySyncTask;
            _sseClientStatusQueue = sseClientStatus;
            _ctsStreaming = new CancellationTokenSource();
            _ctsShutdown = new CancellationTokenSource();
        }

        #region Public Methods
        public void Start()
        {
            _tasksManager.Start(async () =>
            {
                try
                {
                    while (!await _synchronizer.SyncAllAsync())
                    {
                        _wrapperAdapter.TaskDelay(500).Wait();
                    }

                    _statusManager.SetReady();
                    _telemetrySyncTask.RecordConfigInit();
                    _synchronizer.StartPeriodicDataRecording();

                    if (_streamingEnabled)
                    {
                        _log.Debug("Starting streaming mode...");
                        _tasksManager.Start(OnSSEClientStatus, "SSE Client Status");
                        var connected = await _pushManager.StartSse().ConfigureAwait(false);

                        if (connected) return;
                    }

                    _log.Debug("Starting polling mode ...");
                    _synchronizer.StartPeriodicFetching();
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));
                }
                catch (Exception ex)
                {
                    _log.Debug("Exception initialization SDK.", ex);
                }
            }, _ctsShutdown, "SDK Initialization");
        }

        public void Shutdown()
        {
            _synchronizer.StopPeriodicFetching();
            _synchronizer.ClearFetchersCache();
            _synchronizer.StopPeriodicDataRecording();
            _pushManager.StopSse();
            _ctsShutdown.Cancel();
            _ctsShutdown.Dispose();
            if (!_ctsStreaming.IsCancellationRequested) _ctsStreaming.Cancel();
            _ctsStreaming.Dispose();
        }

        public void OnSSEClientStatus()
        {
            try
            {
                while (!_ctsStreaming.IsCancellationRequested)
                {
                    if (_sseClientStatusQueue.TryTake(out SSEClientActions action, -1, _ctsStreaming.Token))
                    {
                        _log.Debug($"OnSSEClientStatus Action: {action}");

                        switch (action)
                        {
                            case SSEClientActions.CONNECTED:
                                ProcessConnected();
                                break;
                            case SSEClientActions.RETRYABLE_ERROR:
                                ProcessDisconnect(retry: true);
                                break;
                            case SSEClientActions.DISCONNECT:
                            case SSEClientActions.NONRETRYABLE_ERROR:
                                ProcessDisconnect(retry: false);
                                break;
                            case SSEClientActions.SUBSYSTEM_DOWN:
                                ProcessSubsystemDown();
                                break;
                            case SSEClientActions.SUBSYSTEM_READY:
                                ProcessSubsystemReady();
                                break;
                            case SSEClientActions.SUBSYSTEM_OFF:
                                ProcessSubsystemOff();
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
            }
            finally
            { _sseClientStatusQueue.Dispose(); }
        }
        #endregion

        #region Private Methods
        private void ProcessConnected()
        {
            lock (_lock)
            {
                if (_streamingConnected)
                {
                    _log.Debug("Streaming already connected.");
                    return;
                }

                _streamingConnected = true;
                _sseHandler.StartWorkers();
                SyncAll(nameof(ProcessConnected));
                _synchronizer.StopPeriodicFetching();
                _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Streaming));
            }
        }

        private void ProcessDisconnect(bool retry)
        {
            lock (_lock)
            {
                if (!_streamingConnected)
                {
                    _log.Debug("Streaming already disconnected.");
                    return;
                }

                _streamingConnected = false;
                _sseHandler.StopWorkers();
                SyncAll(nameof(ProcessDisconnect));
                _synchronizer.StartPeriodicFetching();
                _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));

                if (retry)
                {
                    _pushManager.StartSse();
                }
            }
        }

        private void ProcessSubsystemDown()
        {
            _sseHandler.StopWorkers();
            _synchronizer.StartPeriodicFetching();
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));
        }

        private void ProcessSubsystemReady()
        {
            _synchronizer.StopPeriodicFetching();
            SyncAll(nameof(ProcessSubsystemReady));
            _sseHandler.StartWorkers();
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Streaming));
        }

        private void ProcessSubsystemOff()
        {
            _pushManager.StopSse();
            _ctsStreaming.Cancel();
        }

        private void SyncAll(string method)
        {
            _tasksManager.Start(async() => await _synchronizer.SyncAllAsync(), _ctsShutdown, $"SyncAll - {method}");
        }
        #endregion
    }
}
