using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;

namespace Splitio.Services.EventSource
{
    public class NotificationProcessor : INotificationProcessor
    {
        private readonly ISplitLogger _log;
        private readonly ISplitsWorker _splitsWorker;
        private readonly ISegmentsWorker _segmentsWorker;
        private readonly ISplitCache _splitCache;

        public NotificationProcessor(ISplitsWorker splitsWorker, ISegmentsWorker segmentsWorker, ISplitCache splitCache)
        {
            _log = WrapperAdapter.Instance().GetLogger(typeof(EventSourceClient));
            _splitsWorker = splitsWorker;
            _segmentsWorker = segmentsWorker;
            _splitCache = splitCache;
        }

        public void Proccess(IncomingNotification notification)
        {
            try
            {
                switch (notification.Type)
                {
                    case NotificationType.SPLIT_UPDATE:
                        ProcessSplitUpdate(notification);
                        break;
                    case NotificationType.SPLIT_KILL:
                        ProcessSplitKill(notification);
                        break;
                    case NotificationType.SEGMENT_UPDATE:
                        ProcessSegmentUpdate(notification);
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

        private void ProcessSplitUpdate(IncomingNotification notification)
        {
            var scn = (SplitChangeNotifiaction)notification;

            if (_splitCache.GetChangeNumber() >= scn.ChangeNumber) return;

            if (scn.Split != null && _splitCache.GetChangeNumber() == scn.PreviousChangeNumber)
            {
                _splitCache.AddOrUpdate(scn.Split.name, scn.Split);
                return;
            }

            _splitsWorker.AddToQueue(scn.ChangeNumber);
        }

        private void ProcessSplitKill(IncomingNotification notification)
        {
            var skn = (SplitKillNotification)notification;

            if (skn.ChangeNumber > _splitCache.GetChangeNumber())
            {
                _log.Debug($"Kill Split: {skn.SplitName}, changeNumber: {skn.ChangeNumber} and defaultTreatment: {skn.DefaultTreatment}");
                _splitCache.Kill(skn.ChangeNumber, skn.SplitName, skn.DefaultTreatment);
            }

            _splitsWorker.AddToQueue(skn.ChangeNumber);
        }

        private void ProcessSegmentUpdate(IncomingNotification notification)
        {
            var sc = (SegmentChangeNotification)notification;
            _segmentsWorker.AddToQueue(sc.ChangeNumber, sc.SegmentName);
        }
    }
}
