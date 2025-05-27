using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource.Workers
{
    public class SplitsWorker : BaseWorker, ISplitsWorker, IQueueObserver
    {
        private readonly ISynchronizer _synchronizer;
        private readonly IFeatureFlagCache _featureFlagCache;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly ISelfRefreshingSegmentFetcher _segmentFetcher;
        private readonly IUpdater<Split> _featureFlagUpdater;
        private readonly SplitQueue<InstantUpdateNotification> _queue;
        private readonly IRuleBasedSegmentCache _ruleBasedSegmentCache;
        private readonly IUpdater<RuleBasedSegmentDto> _rbsUpdater;

        public SplitsWorker(ISynchronizer synchronizer,
            IFeatureFlagCache featureFlagCache,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ISelfRefreshingSegmentFetcher segmentFetcher,
            IUpdater<Split> featureFlagUpdater,
            IRuleBasedSegmentCache ruleBasedSegmentCache,
            IUpdater<RuleBasedSegmentDto> rbsUpdater) : base("FeatureFlagsWorker", WrapperAdapter.Instance().GetLogger(typeof(SplitsWorker)))
        {
            _synchronizer = synchronizer;
            _featureFlagCache = featureFlagCache;
            _queue = new SplitQueue<InstantUpdateNotification>();
            _queue.AddObserver(this);
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _segmentFetcher = segmentFetcher;
            _featureFlagUpdater = featureFlagUpdater;
            _ruleBasedSegmentCache = ruleBasedSegmentCache;
            _rbsUpdater = rbsUpdater;
        }

        #region Public Methods
        public async Task AddToQueue(InstantUpdateNotification notification)
        {
            if (!_running)
            {
                _log.Debug("FeatureFlagsWorker is not running and the SDK is trying to process a new notification.");
                return;
            }

            try
            {
                _log.Debug($"Add to queue: {notification.ChangeNumber}");
                await _queue.EnqueueAsync(notification);
            }
            catch (Exception ex)
            {
                _log.Error($"AddToQueue: {ex.Message}");
            }
        }

        public void Kill(SplitKillNotification skn)
        {
            try
            {
                if (skn.ChangeNumber > _featureFlagCache.GetChangeNumber())
                {
                    _log.Debug($"Kill Feature Flag: {skn.SplitName}, changeNumber: {skn.ChangeNumber} and defaultTreatment: {skn.DefaultTreatment}");
                    _featureFlagCache.Kill(skn.ChangeNumber, skn.SplitName, skn.DefaultTreatment);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error killing the following feature flag: {skn.SplitName}", ex);
            }
        }

        public async Task Notify()
        {
            try
            {
                if (!_queue.TryDequeue(out InstantUpdateNotification notification)) return;

                _log.Debug($"ChangeNumber dequeue: {notification.ChangeNumber}, Type: {notification.Type}.");


                var success = false;
                ICacheConsumer consumer = _featureFlagCache;
                switch (notification)
                {
                    case SplitChangeNotification scn:
                        success = await ProcessSplitChangeNotificationAsync(scn);
                        break;
                    case RuleBasedSegmentNotification rbsn:
                        success = await ProcessRuleBasedSegmentNotificationAsync(rbsn);
                        consumer = _ruleBasedSegmentCache;
                        break;
                }

                if (!success)
                    await _synchronizer.SynchronizeSplitsAsync(notification.ChangeNumber, consumer);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;

                _log.Warn($"FeatureFlags ExecuteAsync exception.", ex);
            }
        }
        #endregion

        #region Private Methods
        private async Task<bool> ProcessRuleBasedSegmentNotificationAsync(RuleBasedSegmentNotification notification)
        {
            try
            {
                if (_ruleBasedSegmentCache.GetChangeNumber() >= notification.ChangeNumber) return true;

                if (notification.RuleBasedSegmentDto == null || _ruleBasedSegmentCache.GetChangeNumber() != notification.PreviousChangeNumber)
                    return false;

                var segments = _rbsUpdater.Process(new List<RuleBasedSegmentDto> { notification.RuleBasedSegmentDto }, notification.ChangeNumber);

                if (segments[Enums.SegmentType.Standard].Count > 0)
                    await _segmentFetcher.FetchSegmentsIfNotExistsAsync(segments[Enums.SegmentType.Standard]);

                _log.Debug($"IRBSU, Rule-based Segment updated successfully: {notification.RuleBasedSegmentDto.Name}");
            }
            catch (Exception ex)
            {
                _log.Error($"Somenthing went wrong processing a Rule-based Segment notification", ex);

                return false;
            }

            return true;
        }
        private async Task<bool> ProcessSplitChangeNotificationAsync(SplitChangeNotification scn)
        {
            try
            {
                if (_featureFlagCache.GetChangeNumber() >= scn.ChangeNumber)
                    return true;

                if (scn.FeatureFlag == null || _featureFlagCache.GetChangeNumber() != scn.PreviousChangeNumber)
                    return false;

                var segments = _featureFlagUpdater.Process(new List<Split> { scn.FeatureFlag }, scn.ChangeNumber);

                if (segments[Enums.SegmentType.Standard].Count > 0)
                    await _segmentFetcher.FetchSegmentsIfNotExistsAsync(segments[Enums.SegmentType.Standard]);

                if (segments[Enums.SegmentType.RuleBased].Count >0 && !_ruleBasedSegmentCache.Contains(segments[Enums.SegmentType.RuleBased]))
                    return false;

                _telemetryRuntimeProducer.RecordUpdatesFromSSE(UpdatesFromSSEEnum.Splits);

                _log.Debug($"IFFU, Feature Flag updated successfully: {scn.FeatureFlag.name}");
            }
            catch (Exception ex)
            {
                _log.Error($"Somenthing went wrong processing a Feature Flag notification", ex);

                return false;
            }

            return true;
        }
        #endregion
    }
}
