using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource.Workers
{
    public class SplitsWorker : BaseWorker, ISplitsWorker
    {
        private readonly ISynchronizer _synchronizer;
        private readonly ISplitCache _featureFlagCache;
        private readonly ISplitParser _featureFlagParser;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly ISelfRefreshingSegmentFetcher _segmentFetcher;
        private readonly BlockingCollection<SplitChangeNotification> _queue;

        public SplitsWorker(ISynchronizer synchronizer,
            ISplitCache featureFlagCache,
            ISplitParser featureFlagParser,
            BlockingCollection<SplitChangeNotification>  queue,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            ISelfRefreshingSegmentFetcher segmentFetcher,
            ISplitTask task) : base("FeatureFlagsWorker", WrapperAdapter.Instance().GetLogger(typeof(SplitsWorker)), task)
        {
            _synchronizer = synchronizer;
            _featureFlagCache = featureFlagCache;
            _featureFlagParser = featureFlagParser;
            _queue = queue;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _segmentFetcher = segmentFetcher;
        }

        #region Public Methods
        public void AddToQueue(SplitChangeNotification scn)
        {
            try
            {
                _log.Debug($"Add to queue: {scn.ChangeNumber}");
                _queue.TryAdd(scn);
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
        #endregion

        #region Protected Methods
        protected override async Task ExecuteAsync()
        {
            _log.Debug($"FeatureFlags Worker, Token: {_cts.IsCancellationRequested}; Running: {_task.IsRunning()}.");
            try
            {
                if (_queue.TryTake(out SplitChangeNotification scn, -1, _cts.Token))
                {
                    _log.Debug($"ChangeNumber dequeue: {scn.ChangeNumber}");

                    var success = await ProcessSplitChangeNotification(scn);

                    if (!success) await _synchronizer.SynchronizeSplitsAsync(scn.ChangeNumber);
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) return;

                _log.Warn($"FeatureFlags ExecuteAsync exception.", ex);
            }
        }

        private async Task<bool> ProcessSplitChangeNotification(SplitChangeNotification scn)
        {
            try
            {
                if (_featureFlagCache.GetChangeNumber() >= scn.ChangeNumber)
                    return true;

                if (scn.FeatureFlag == null || _featureFlagCache.GetChangeNumber() != scn.PreviousChangeNumber)
                    return false;

                var ffParsed = _featureFlagParser.Parse(scn.FeatureFlag);

                // if ffParsed is null it means that the status is ARCHIVED or different to ACTIVE.
                if (ffParsed == null)
                {
                    _featureFlagCache.RemoveSplit(scn.FeatureFlag.name);
                }
                else
                {
                    _featureFlagCache.AddOrUpdate(scn.FeatureFlag.name, ffParsed);
                    var segmentNames = Util.Helper.GetSegmentNamesBySplit(scn.FeatureFlag);

                    if (segmentNames.Count > 0)
                        await _segmentFetcher.FetchSegmentsIfNotExists(segmentNames);
                }

                _featureFlagCache.SetChangeNumber(scn.ChangeNumber);
                _telemetryRuntimeProducer.RecordUpdatesFromSSE(UpdatesFromSSEEnum.Splits);

                return true;

            }
            catch (Exception ex)
            {
                _log.Error($"Somenthing went wrong processing a Feature Flag notification", ex);
            }

            return false;
        }
        #endregion
    }
}
