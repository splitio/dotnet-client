using Splitio.Services.EventSource.Workers;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public class NotificationProcessor : INotificationProcessor
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(EventSourceClient));

        private readonly ISplitsWorker _featureFlagsWorker;
        private readonly ISegmentsWorker _segmentsWorker;

        public NotificationProcessor(ISplitsWorker featureFlagsWorker, ISegmentsWorker segmentsWorker)
        {
            _featureFlagsWorker = featureFlagsWorker;
            _segmentsWorker = segmentsWorker;
        }

        public async Task Proccess(IncomingNotification notification)
        {
            try
            {
                switch (notification.Type)
                {
                    case NotificationType.SPLIT_UPDATE:
                        await _featureFlagsWorker.AddToQueue((SplitChangeNotification)notification);
                        break;
                    case NotificationType.SPLIT_KILL:
                        var skn = (SplitKillNotification)notification;
                        _featureFlagsWorker.Kill(skn);
                        await _featureFlagsWorker.AddToQueue(new SplitChangeNotification { ChangeNumber = skn.ChangeNumber });
                        break;
                    case NotificationType.SEGMENT_UPDATE:
                        var sc = (SegmentChangeNotification)notification;
                        await _segmentsWorker.AddToQueue(sc.ChangeNumber, sc.SegmentName);
                        break;
                    case NotificationType.RB_SEGMENT_UPDATE:
                        await _featureFlagsWorker.AddToQueue((RuleBasedSegmentNotification)notification);
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
