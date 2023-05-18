using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using System;

namespace Splitio.Services.EventSource
{
    public class NotificationProcessor : INotificationProcessor
    {
        private readonly ISplitLogger _log;
        private readonly ISplitsWorker _featureFlagsWorker;
        private readonly ISegmentsWorker _segmentsWorker;
        private readonly ISplitCache _featureFlagCache;
        private readonly ISplitParser _featureFlagParser;

        public NotificationProcessor(ISplitsWorker featureFlagsWorker, ISegmentsWorker segmentsWorker, ISplitCache featureFlagCache, ISplitParser featureFlagParser)
        {
            _log = WrapperAdapter.Instance().GetLogger(typeof(EventSourceClient));
            _featureFlagsWorker = featureFlagsWorker;
            _segmentsWorker = segmentsWorker;
            _featureFlagCache = featureFlagCache;
            _featureFlagParser = featureFlagParser;
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
            var scn = (SplitChangeNotification)notification;

            if (_featureFlagCache.GetChangeNumber() >= scn.ChangeNumber) return;

            if (scn.FeatureFlag != null && _featureFlagCache.GetChangeNumber() == scn.PreviousChangeNumber)
            {
                _featureFlagCache.AddOrUpdate(scn.FeatureFlag.name, _featureFlagParser.Parse(scn.FeatureFlag));
                return;
            }

            _featureFlagsWorker.AddToQueue(scn.ChangeNumber);
        }

        private void ProcessSplitKill(IncomingNotification notification)
        {
            var skn = (SplitKillNotification)notification;

            if (skn.ChangeNumber > _featureFlagCache.GetChangeNumber())
            {
                _log.Debug($"Kill Split: {skn.SplitName}, changeNumber: {skn.ChangeNumber} and defaultTreatment: {skn.DefaultTreatment}");
                _featureFlagCache.Kill(skn.ChangeNumber, skn.SplitName, skn.DefaultTreatment);
            }

            _featureFlagsWorker.AddToQueue(skn.ChangeNumber);
        }

        private void ProcessSegmentUpdate(IncomingNotification notification)
        {
            var sc = (SegmentChangeNotification)notification;
            _segmentsWorker.AddToQueue(sc.ChangeNumber, sc.SegmentName);
        }
    }
}
