﻿using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;

namespace Splitio.Services.EventSource
{
    public class NotificationManagerKeeper : INotificationManagerKeeper
    {
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly ISplitLogger _log;
        private readonly object _eventOccupancyLock = new object();

        private bool _publisherAvailable;
        private int _publishersPri;
        private int _publishersSec;

        public event EventHandler<SSEActionsEventArgs> ActionEvent;

        public NotificationManagerKeeper(ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ISplitLogger log = null)
        {
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _log = log ?? WrapperAdapter.GetLogger(typeof(NotificationManagerKeeper));

            _publisherAvailable = true;
        }

        #region Public Methods
        public void HandleIncomingEvent(IncomingNotification notification)
        {
            switch (notification.Type)
            {
                case NotificationType.CONTROL:
                    ProcessEventControl(notification);
                    break;
                case NotificationType.OCCUPANCY:
                    ProcessEventOccupancy(notification);
                    break;
                default:
                    _log.Error($"Incorrect notification type: {notification.Type}");
                    break;
            }
        }
        #endregion

        #region Private Methods
        private void ProcessEventControl(IncomingNotification notification)
        {            
            var controlEvent = (ControlNotification)notification;

            switch (controlEvent.ControlType)
            {
                case ControlType.STREAMING_PAUSED:
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.StreamingStatus, (int)StreamingStatusEnum.Paused));
                    DispatchActionEvent(SSEClientActions.SUBSYSTEM_DOWN);
                    break;
                case ControlType.STREAMING_RESUMED:
                    lock (_eventOccupancyLock)
                    {
                        _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.StreamingStatus, (int)StreamingStatusEnum.Enabled));
                        if (_publisherAvailable) DispatchActionEvent(SSEClientActions.SUBSYSTEM_READY);
                    }
                    break;
                case ControlType.STREAMING_DISABLED:
                    _telemetryRuntimeProducer.RecordStreamingEvent(new StreamingEvent(EventTypeEnum.StreamingStatus, (int)StreamingStatusEnum.Disabled));
                    DispatchActionEvent(SSEClientActions.SUBSYSTEM_OFF);
                    break;
                default:
                    _log.Error($"Incorrect control type. {controlEvent.ControlType}");
                    break;
            }
        }

        private void ProcessEventOccupancy(IncomingNotification notification)
        {
            lock (_eventOccupancyLock)
            {
                var occupancyEvent = (OccupancyNotification)notification;

                UpdatePublishers(occupancyEvent.Channel, occupancyEvent.Metrics.Publishers);

                if (!ArePublishersAvailable() && _publisherAvailable)
                {
                    _publisherAvailable = false;
                    DispatchActionEvent(SSEClientActions.SUBSYSTEM_DOWN);
                }
                else if (ArePublishersAvailable() && !_publisherAvailable)
                {
                    _publisherAvailable = true;
                    DispatchActionEvent(SSEClientActions.SUBSYSTEM_READY);
                }
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

        private void DispatchActionEvent(SSEClientActions action)
        {
            ActionEvent?.Invoke(this, new SSEActionsEventArgs(action));
        }
        #endregion
    }
}
