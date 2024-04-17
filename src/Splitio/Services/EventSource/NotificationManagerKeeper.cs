using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public class NotificationManagerKeeper : INotificationManagerKeeper
    {
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly ISplitLogger _log;
        private readonly SplitQueue<StreamingStatus> _streamingStatusQueue;
        private readonly object _getAndSetStreaming = new object();
        private readonly object _getAndSetControl = new object();

        private bool _publisherAvailable;
        private int _publishersPri;
        private int _publishersSec;

        private SSEClientStatusMessage _currentStatus;
        private ControlType _backendStatus;

        public NotificationManagerKeeper(ITelemetryRuntimeProducer telemetryRuntimeProducer, SplitQueue<StreamingStatus> streamingStatusQueue)
        {
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _streamingStatusQueue = streamingStatusQueue; 
            _log = WrapperAdapter.Instance().GetLogger(typeof(NotificationManagerKeeper));
            Reset();
        }

        #region Public Methods
        public async Task HandleSseStatus(SSEClientStatusMessage newStatus)
        {
            _log.Debug($"New streaming status message received: {newStatus}. Current status: {_currentStatus}.");

            switch (newStatus)
            {
                case SSEClientStatusMessage.INITIALIZATION_IN_PROGRESS:
                    Reset();
                    break;
                case SSEClientStatusMessage.CONNECTED:
                    CompareAndSet(SSEClientStatusMessage.INITIALIZATION_IN_PROGRESS, newStatus);
                    break;
                case SSEClientStatusMessage.FIRST_EVENT:
                    if (_currentStatus.Equals(SSEClientStatusMessage.CONNECTED))
                    {
                        _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.SSEConnectionEstablished));
                        await _streamingStatusQueue.EnqueueAsync(StreamingStatus.STREAMING_READY);
                    }
                    break;
                case SSEClientStatusMessage.RETRYABLE_ERROR:
                    if (CompareAndSet(SSEClientStatusMessage.INITIALIZATION_IN_PROGRESS, newStatus) ||
                        CompareAndSet(SSEClientStatusMessage.CONNECTED, newStatus) ||
                        CompareAndSet(SSEClientStatusMessage.RETRYABLE_ERROR, newStatus) ||
                        CompareAndSet(SSEClientStatusMessage.FORCED_STOP, newStatus))
                    {
                        await _streamingStatusQueue.EnqueueAsync(StreamingStatus.STREAMING_BACKOFF);
                    }
                    break;
                case SSEClientStatusMessage.NONRETRYABLE_ERROR:
                    if (CompareAndSet(SSEClientStatusMessage.CONNECTED, newStatus) ||
                        CompareAndSet(SSEClientStatusMessage.RETRYABLE_ERROR, newStatus))
                    {
                        await _streamingStatusQueue.EnqueueAsync(StreamingStatus.STREAMING_OFF);
                    }
                    break;
                case SSEClientStatusMessage.FORCED_STOP:
                    if (CompareAndSet(SSEClientStatusMessage.INITIALIZATION_IN_PROGRESS, newStatus) ||
                        CompareAndSet(SSEClientStatusMessage.CONNECTED, newStatus) ||
                        CompareAndSet(SSEClientStatusMessage.RETRYABLE_ERROR, newStatus))
                    {
                        await _streamingStatusQueue.EnqueueAsync(StreamingStatus.STREAMING_DOWN);
                    }
                    break;
                default:
                    _log.Info($"HandleSseStatus: Unrecognized status - {newStatus}");
                    break;
            }
        }

        public async Task HandleIncomingEvent(IncomingNotification notification)
        {
            switch (notification.Type)
            {
                case NotificationType.CONTROL:
                    await ProcessEventControlAsync(notification);
                    break;
                case NotificationType.OCCUPANCY:
                    await ProcessEventOccupancyAsync(notification);
                    break;
                default:
                    _log.Error($"Incorrect notification type: {notification.Type}");
                    break;
            }
        }
        #endregion

        #region Private Methods
        private async Task ProcessEventControlAsync(IncomingNotification notification)
        {
            if (_backendStatus.Equals(ControlType.STREAMING_DISABLED)) return;

            var controlEvent = (ControlNotification)notification;

            switch (controlEvent.ControlType)
            {
                case ControlType.STREAMING_PAUSED:
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.StreamingStatus, (int)StreamingStatusEnum.Paused));

                    if (CompareAndSet(ControlType.STREAMING_RESUMED, controlEvent.ControlType) && _publisherAvailable) // If there are no publishers online, the STREAMING_DOWN message should have already been sent
                    {
                        await _streamingStatusQueue.EnqueueAsync(StreamingStatus.STREAMING_DOWN);
                    }
                    break;
                case ControlType.STREAMING_RESUMED:
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.StreamingStatus, (int)StreamingStatusEnum.Enabled));

                    if (CompareAndSet(ControlType.STREAMING_PAUSED, controlEvent.ControlType) && _publisherAvailable)
                    {
                        await _streamingStatusQueue.EnqueueAsync(StreamingStatus.STREAMING_READY);
                    }
                    break;
                case ControlType.STREAMING_DISABLED:
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.StreamingStatus, (int)StreamingStatusEnum.Disabled));

                    _backendStatus = ControlType.STREAMING_DISABLED;
                    await _streamingStatusQueue.EnqueueAsync(StreamingStatus.STREAMING_OFF);
                    break;
                default:
                    _log.Error($"Incorrect control type. {controlEvent.ControlType}");
                    break;
            }
        }

        private async Task ProcessEventOccupancyAsync(IncomingNotification notification)
        {
            var occupancyEvent = (OccupancyNotification)notification;

            UpdatePublishers(occupancyEvent.Channel, occupancyEvent.Metrics.Publishers);

            if (!ArePublishersAvailable() && _publisherAvailable && _backendStatus.Equals(ControlType.STREAMING_RESUMED))
            {
                _publisherAvailable = false;
                await _streamingStatusQueue.EnqueueAsync(StreamingStatus.STREAMING_DOWN);
            }
            else if (ArePublishersAvailable() && !_publisherAvailable && _backendStatus.Equals(ControlType.STREAMING_RESUMED))
            {
                _publisherAvailable = true;
                await _streamingStatusQueue.EnqueueAsync(StreamingStatus.STREAMING_READY);
            }
        }

        private void UpdatePublishers(string channel, int publishers)
        {
            if (channel.Equals(Constants.Push.ControlPri))
            {
                _publishersPri = publishers;
                _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.OccupancyPri, publishers));
                return;
            }

            if (channel.Equals(Constants.Push.ControlSec))
            {
                _publishersSec = publishers;
                _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.OccupancySec, publishers));
                return;
            }
        }

        private bool ArePublishersAvailable()
        {
            return _publishersPri >= 1 || _publishersSec >= 1;
        }

        private void Reset()
        {
            lock (_getAndSetStreaming)
            {
                _currentStatus = SSEClientStatusMessage.INITIALIZATION_IN_PROGRESS;
            }

            lock (_getAndSetControl)
            {
                _publisherAvailable = true;
                _backendStatus = ControlType.STREAMING_RESUMED;
                _publishersPri = 2;
                _publishersSec = 2;
            }
        }

        private bool CompareAndSet(SSEClientStatusMessage expected, SSEClientStatusMessage newStatus)
        {
            lock (_getAndSetStreaming)
            {
                if (_currentStatus.Equals(expected))
                {
                    _currentStatus = newStatus;
                    return true;
                }

                return false;
            }
        }

        private bool CompareAndSet(ControlType expected, ControlType newStatus)
        {
            lock (_getAndSetControl)
            {
                if (_backendStatus.Equals(expected))
                {
                    _backendStatus = newStatus;
                    return true;
                }

                return false;
            }
        }
        #endregion
    }
}
