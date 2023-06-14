using Splitio.Services.EventSource.Workers;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;

namespace Splitio.Services.EventSource
{
    public class NotificationProcessor : INotificationProcessor
    {
        private readonly ISplitLogger _log;
        private readonly ISplitsWorker _featureFlagsWorker;
        private readonly ISegmentsWorker _segmentsWorker;

        public NotificationProcessor(ISplitsWorker featureFlagsWorker, ISegmentsWorker segmentsWorker)
        {
            _log = WrapperAdapter.Instance().GetLogger(typeof(EventSourceClient));
            _featureFlagsWorker = featureFlagsWorker;
            _segmentsWorker = segmentsWorker;
        }

        public void Proccess(IncomingNotification notification)
        {
            try
            {
                switch (notification.Type)
                {
                    case NotificationType.SPLIT_UPDATE:
                        _featureFlagsWorker.AddToQueue((SplitChangeNotification)notification);
                        break;
                    case NotificationType.SPLIT_KILL:
                        var skn = (SplitKillNotification)notification;
                        _featureFlagsWorker.Kill(skn);
                        _featureFlagsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = skn.ChangeNumber });
                        break;
                    case NotificationType.SEGMENT_UPDATE:
                        var sc = (SegmentChangeNotification)notification;
                        _segmentsWorker.AddToQueue(sc.ChangeNumber, sc.SegmentName);
                        break;
                    default:
                        _log.Debug($"Incorrect Event type: {notification}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Processor: {ex.Message}");
            }
        }
    }
}
