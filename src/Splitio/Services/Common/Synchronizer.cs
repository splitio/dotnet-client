using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Telemetry.Common;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class Synchronizer : ISynchronizer
    {
        private readonly static int OnDemandFetchBackoffMaxRetries = 10;

        private readonly ISplitFetcher _splitFetcher;
        private readonly ISelfRefreshingSegmentFetcher _segmentFetcher;
        private readonly IImpressionsLog _impressionsLog;
        private readonly IEventsLog _eventsLog;
        private readonly ISplitLogger _log;
        private readonly IImpressionsCounter _impressionsCounter;
        private readonly IStatusManager _statusManager;
        private readonly ITelemetrySyncTask _telemetrySyncTask;
        private readonly ISplitCache _splitCache;
        private readonly ISegmentCache _segmentCache;
        private readonly IBackOff _backOffFeatureFlags;
        private readonly IBackOff _backOffSegments;
        private readonly IUniqueKeysTracker _uniqueKeysTracker;
        private readonly int _onDemandFetchMaxRetries;
        private readonly int _onDemandFetchRetryDelayMs;
        private readonly FetchOptions _defaultFetchOptions;

        public Synchronizer(ISplitFetcher splitFetcher,
            ISelfRefreshingSegmentFetcher segmentFetcher,
            IImpressionsLog impressionsLog,
            IEventsLog eventsLog,
            IImpressionsCounter impressionsCounter,
            IStatusManager statusManager,
            ITelemetrySyncTask telemetrySyncTask,
            ISplitCache splitCache,
            IBackOff backOffFeatureFlags,
            IBackOff backOffSegments,
            int onDemandFetchMaxRetries,
            int onDemandFetchRetryDelayMs,
            ISegmentCache segmentCache,
            IUniqueKeysTracker uniqueKeysTracker,
            ISplitLogger log = null)
        {
            _splitFetcher = splitFetcher;
            _segmentFetcher = segmentFetcher;
            _impressionsLog = impressionsLog;
            _eventsLog = eventsLog;
            _impressionsCounter = impressionsCounter;
            _statusManager = statusManager;
            _telemetrySyncTask = telemetrySyncTask;
            _splitCache = splitCache;
            _backOffFeatureFlags = backOffFeatureFlags;
            _backOffSegments = backOffSegments;
            _onDemandFetchMaxRetries = onDemandFetchMaxRetries;
            _onDemandFetchRetryDelayMs = onDemandFetchRetryDelayMs;
            _segmentCache = segmentCache;
            _uniqueKeysTracker = uniqueKeysTracker;
            _log = log ?? WrapperAdapter.Instance().GetLogger(typeof(Synchronizer));
            _defaultFetchOptions = new FetchOptions();
        }

        #region Public Methods
        public void StartPeriodicDataRecording()
        {
            if (_statusManager.IsDestroyed()) return;

            _telemetrySyncTask.Start();
            _impressionsLog.Start();
            _eventsLog.Start();
            _impressionsCounter.Start();
            _uniqueKeysTracker.Start();
        }

        public void StartPeriodicFetching()
        {
            if (_statusManager.IsDestroyed()) return;

            _splitFetcher.Start();
            _segmentFetcher.Start();
        }

        public async Task StopPeriodicDataRecordingAsync()
        {
            await _telemetrySyncTask.StopAsync();
            await _impressionsLog.StopAsync();
            await _eventsLog.StopAsync();
            await _impressionsCounter.StopAsync();
            await _uniqueKeysTracker.StopAsync();
        }

        public async Task StopPeriodicFetchingAsync()
        {
            await _splitFetcher.StopAsync();
            await _segmentFetcher.StopAsync();
        }

        public void ClearFetchersCache()
        {
            _splitFetcher.Clear();
            _segmentFetcher.Clear();
        }

        public async Task<bool> SyncAllAsync()
        {
            var splits = await _splitFetcher.FetchSplitsAsync(_defaultFetchOptions);
            var segments = await _segmentFetcher.FetchAllAsync();

            return splits.Success && segments;
        }

        public async Task SynchronizeSegmentAsync(string segmentName, long targetChangeNumber)
        {
            try
            {
                if (targetChangeNumber <= _segmentCache.GetChangeNumber(segmentName)) return;

                var fetchOptions = new FetchOptions { CacheControlHeaders = true };

                var result = await AttemptSegmentAsync(segmentName, targetChangeNumber, fetchOptions, _onDemandFetchMaxRetries, _onDemandFetchRetryDelayMs, false);

                if (result.Success)
                {
                    _log.Debug($"Segment {segmentName} refresh completed in {_onDemandFetchMaxRetries - result.RemainingAttempts} attempts.");

                    return;
                }

                fetchOptions.Till = targetChangeNumber;

                var withCDNBypassed = await AttemptSegmentAsync(segmentName, targetChangeNumber, fetchOptions, OnDemandFetchBackoffMaxRetries, null, true);

                if (withCDNBypassed.Success)
                {
                    _log.Debug($"Segment {segmentName} refresh completed bypassing the CDN in {OnDemandFetchBackoffMaxRetries - withCDNBypassed.RemainingAttempts} attempts.");
                }
                else
                {
                    _log.Debug($"No changes fetched for segment {segmentName} after {OnDemandFetchBackoffMaxRetries - withCDNBypassed.RemainingAttempts} attempts with CDN bypassed.");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Exception caught executing SynchronizeSegment: {segmentName}-{targetChangeNumber}", ex);
            }
        }

        public async Task SynchronizeSplitsAsync(long targetChangeNumber)
        {
            try
            {
                if (targetChangeNumber <= _splitCache.GetChangeNumber()) return;

                var fetchOptions = new FetchOptions { CacheControlHeaders = true };

                var result = await AttemptSplitsAsync(targetChangeNumber, fetchOptions, _onDemandFetchMaxRetries, _onDemandFetchRetryDelayMs, false);

                if (result.Success)
                {
                    await _segmentFetcher.FetchSegmentsIfNotExistsAsync(result.SegmentNames);
                    _log.Debug($"Refresh completed in {_onDemandFetchMaxRetries - result.RemainingAttempts} attempts.");

                    return;
                }

                fetchOptions.Till = targetChangeNumber;
                var withCDNBypassed = await AttemptSplitsAsync(targetChangeNumber, fetchOptions, OnDemandFetchBackoffMaxRetries, null, true);

                if (withCDNBypassed.Success)
                {
                    await _segmentFetcher.FetchSegmentsIfNotExistsAsync(withCDNBypassed.SegmentNames);
                    _log.Debug($"Refresh completed bypassing the CDN in {OnDemandFetchBackoffMaxRetries - withCDNBypassed.RemainingAttempts} attempts.");
                }
                else
                {
                    _log.Debug($"No changes fetched after {OnDemandFetchBackoffMaxRetries - withCDNBypassed.RemainingAttempts} attempts with CDN bypassed.");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Exception caught executing SynchronizeSplits. {targetChangeNumber}", ex);
            }
        }
        #endregion

        #region Private Methods
        private async Task<SyncResult> AttemptSegmentAsync(string name, long targetChangeNumber, FetchOptions fetchOptions, int maxRetries, int? retryDelayMs, bool withBackoff)
        {
            try
            {
                var remainingAttempts = maxRetries;

                if (withBackoff) _backOffSegments.Reset();

                while (true)
                {
                    remainingAttempts--;
                    await _segmentFetcher.FetchAsync(name, fetchOptions);

                    if (targetChangeNumber <= _segmentCache.GetChangeNumber(name))
                    {
                        return new SyncResult(true, remainingAttempts);
                    }
                    else if (remainingAttempts <= 0)
                    {
                        return new SyncResult(false, remainingAttempts);
                    }

                    var delay = withBackoff ? _backOffSegments.GetInterval(inMiliseconds: true) : retryDelayMs.Value;
                    await Task.Delay((int)delay);
                }
            }
            catch (Exception ex)
            {
                _log.Debug("Exception while AttemptSegmentAsync.", ex);
            }

            return new SyncResult(false, 0);
        }

        private async Task<SyncResult> AttemptSplitsAsync(long targetChangeNumber, FetchOptions fetchOptions, int maxRetries, int? retryDelayMs, bool withBackoff)
        {
            try
            {
                var remainingAttempts = maxRetries;

                if (withBackoff) _backOffFeatureFlags.Reset();

                while (true)
                {
                    remainingAttempts--;
                    var result = await _splitFetcher.FetchSplitsAsync(fetchOptions);

                    if (targetChangeNumber <= _splitCache.GetChangeNumber())
                    {
                        return new SyncResult(true, remainingAttempts, result.SegmentNames);
                    }
                    else if (remainingAttempts <= 0)
                    {
                        return new SyncResult(false, remainingAttempts, result.SegmentNames);
                    }

                    var delay = withBackoff ? _backOffFeatureFlags.GetInterval(inMiliseconds: true) : retryDelayMs.Value;
                    await Task.Delay((int)delay);
                }
            }
            catch (Exception ex)
            {
                _log.Debug("Exception while AttemptSplitsAsync.", ex);
            }

            return new SyncResult(false, 0);
        }
        #endregion
    }
}
