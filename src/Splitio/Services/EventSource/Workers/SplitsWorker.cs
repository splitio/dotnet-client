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
        private readonly IFeatureFlagSyncService _featureFlagSyncService;
        private readonly SplitQueue<SplitChangeNotification> _queue;

        public SplitsWorker(ISynchronizer synchronizer,
            IFeatureFlagCache featureFlagCache,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ISelfRefreshingSegmentFetcher segmentFetcher,
            IFeatureFlagSyncService featureFlagSyncService) : base("FeatureFlagsWorker", WrapperAdapter.Instance().GetLogger(typeof(SplitsWorker)))
        {
            _synchronizer = synchronizer;
            _featureFlagCache = featureFlagCache;
            _queue = new SplitQueue<SplitChangeNotification>();
            _queue.AddObserver(this);
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _segmentFetcher = segmentFetcher;
            _featureFlagSyncService = featureFlagSyncService;
        }

        #region Public Methods
        public async Task AddToQueue(SplitChangeNotification scn)
        {
            if (!_running)
            {
                _log.Debug("FeatureFlagsWorker is not running and the SDK is trying to process a new notification.");
                return;
            }

            try
            {
                _log.Debug($"Add to queue: {scn.ChangeNumber}");
                await _queue.EnqueueAsync(scn);
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
                if (!_queue.TryDequeue(out SplitChangeNotification scn)) return;

                _log.Debug($"ChangeNumber dequeue: {scn.ChangeNumber}");

                var success = await ProcessSplitChangeNotificationAsync(scn);

                if (!success) await _synchronizer.SynchronizeSplitsAsync(scn.ChangeNumber);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;

                _log.Warn($"FeatureFlags ExecuteAsync exception.", ex);
            }
        }
        #endregion

        #region Private Methods
        private async Task<bool> ProcessSplitChangeNotificationAsync(SplitChangeNotification scn)
        {
            try
            {
                if (_featureFlagCache.GetChangeNumber() >= scn.ChangeNumber)
                    return true;

                if (scn.FeatureFlag == null || _featureFlagCache.GetChangeNumber() != scn.PreviousChangeNumber)
                    return false;

                var sNames = _featureFlagSyncService.UpdateFeatureFlagsFromChanges(new List<Split> { scn.FeatureFlag }, scn.ChangeNumber);

                if (sNames.Count > 0) await _segmentFetcher.FetchSegmentsIfNotExistsAsync(sNames);

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
