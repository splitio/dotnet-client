using Splitio.Services.EventSource;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
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

        private bool _streamingConnected;

        public SyncManager(bool streamingEnabled,
            ISynchronizer synchronizer,
            IPushManager pushManager,
            ISSEHandler sseHandler,
            INotificationManagerKeeper notificationManagerKeeper,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ISplitLogger log = null)
        {
            _streamingEnabled = streamingEnabled;
            _synchronizer = synchronizer;
            _pushManager = pushManager;
            _sseHandler = sseHandler;
            _log = log ?? WrapperAdapter.GetLogger(typeof(Synchronizer));
            _telemetryRuntimeProducer = telemetryRuntimeProducer;

            _sseHandler.ActionEvent += OnProcessFeedbackSSE;
            notificationManagerKeeper.ActionEvent += OnProcessFeedbackSSE;
        }

        #region Public Methods
        public void Start()
        {
            if (_streamingEnabled)
            {
                 StartStream();
            }
            else
            {
                StartPoll();
            }
        }

        public void Shutdown()
        {
            _synchronizer.StopPeriodicFetching();
            _synchronizer.ClearFetchersCache();
            _synchronizer.StopPeriodicDataRecording();
            _pushManager.StopSse();
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
        private void StartPoll()
        {
            _log.Debug("Starting polling mode ...");            

            _synchronizer.StartPeriodicFetching();
            _synchronizer.StartPeriodicDataRecording();
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));
        }

        private void StartStream()
        {
            _log.Debug("Starting streaming mode...");            

            _synchronizer.StartPeriodicDataRecording();
            _synchronizer.SyncAll();
            Task.Factory.StartNew(async () =>
            {
                if (!await _pushManager.StartSse())
                {
                    _synchronizer.StartPeriodicFetching();
                }
            });
        }        

        private void ProcessConnected()
        {
            if (_streamingConnected)
            {
                _log.Debug("Streaming already connected.");
                return;
            }
            
            _streamingConnected = true;
            _sseHandler.StartWorkers();
            _synchronizer.SyncAll();
            _synchronizer.StopPeriodicFetching();
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Streaming));
        }

        private void ProcessDisconnect(bool retry)
        {
            if (!_streamingConnected)
            {
                _log.Debug("Streaming already disconnected.");
                return;
            }

            _streamingConnected = false;
            _sseHandler.StopWorkers();
            _synchronizer.SyncAll();
            _synchronizer.StartPeriodicFetching();
            _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SyncMode, (int)SyncModeEnum.Polling));

            if (retry)
            {
                _pushManager.StartSse();
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
            _synchronizer.SyncAll();
            _sseHandler.StartWorkers();
        }

        private void ProcessSubsystemOff()
        {
            _pushManager.StopSse();
        }
        #endregion
    }
}
