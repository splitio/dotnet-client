﻿using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EventSource;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
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
        private readonly CancellationTokenSource _shutdownCancellationTokenSource;
        private readonly object _lock = new object();

        private bool _streamingConnected;

        public SyncManager(bool streamingEnabled,
            ISynchronizer synchronizer,
            IPushManager pushManager,
            ISSEHandler sseHandler,
            INotificationManagerKeeper notificationManagerKeeper,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            IStatusManager statusManager,
            ITasksManager tasksManager,
            IWrapperAdapter wrapperAdapter,
            ITelemetrySyncTask telemetrySyncTask)
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

            _sseHandler.ActionEvent += OnProcessFeedbackSSE;
            notificationManagerKeeper.ActionEvent += OnProcessFeedbackSSE;

            _shutdownCancellationTokenSource = new CancellationTokenSource();
        }

        #region Public Methods
        public void Start()
        {
            _tasksManager.Start(() =>
            {
                try
                {
                    while (!_synchronizer.SyncAll(_shutdownCancellationTokenSource, asynchronous: false))
                    {
                        _wrapperAdapter.TaskDelay(500).Wait();
                    }

                    _statusManager.SetReady();
                    _telemetrySyncTask.RecordConfigInit();
                    _synchronizer.StartPeriodicDataRecording();

                    if (_streamingEnabled)
                    {
                        _log.Debug("Starting streaming mode...");
                        var connected = _pushManager.StartSse().Result;

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
                
            }, _shutdownCancellationTokenSource, "SDK Initialization");
        }

        public void Shutdown()
        {
            _synchronizer.StopPeriodicFetching();
            _synchronizer.ClearFetchersCache();
            _synchronizer.StopPeriodicDataRecording();
            _pushManager.StopSse();
            _shutdownCancellationTokenSource.Cancel();
            _shutdownCancellationTokenSource.Dispose();
        }

        // public for tests
        public void OnProcessFeedbackSSE(object sender, SSEActionsEventArgs e)
        {
            _log.Debug($"OnProcessFeedbackSSE Action: {e.Action}");

            switch (e.Action)
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
                _synchronizer.SyncAll(_shutdownCancellationTokenSource);
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
                _synchronizer.SyncAll(_shutdownCancellationTokenSource);
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
            _synchronizer.SyncAll(_shutdownCancellationTokenSource);
            _sseHandler.StartWorkers();
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Streaming));
        }

        private void ProcessSubsystemOff()
        {
            _pushManager.StopSse();
        }
        #endregion
    }
}
