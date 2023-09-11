﻿using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Telemetry.Common;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class Synchronizer : ISynchronizer
    {
        private readonly static int OnDemandFetchBackoffMaxRetries = 10;
        private readonly static ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(Synchronizer));

        private readonly ISplitFetcher _splitFetcher;
        private readonly ISelfRefreshingSegmentFetcher _segmentFetcher;
        private readonly IImpressionsLog _impressionsLog;
        private readonly IEventsLog _eventsLog;
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly IImpressionsCounter _impressionsCounter;
        private readonly ITelemetrySyncTask _telemetrySyncTask;
        private readonly IFeatureFlagCacheConsumer _featureFlagCacheConsumer;
        private readonly ISegmentCacheConsumer _segmentCacheConsumer;
        private readonly IBackOff _splitsBackOff;
        private readonly IBackOff _segmentsBackOff;
        private readonly IUniqueKeysTracker _uniqueKeysTracker;
        private readonly int _onDemandFetchMaxRetries;
        private readonly int _onDemandFetchRetryDelayMs;
        private readonly FetchOptions _defaultFetchOptions;

        public Synchronizer(ISplitFetcher splitFetcher,
            ISelfRefreshingSegmentFetcher segmentFetcher,
            IImpressionsLog impressionsLog,
            IEventsLog eventsLog,
            IImpressionsCounter impressionsCounter,
            IWrapperAdapter wrapperAdapter,
            ITelemetrySyncTask telemetrySyncTask,
            IFeatureFlagCacheConsumer featureFlagCacheConsumer,
            IBackOff splitsBackOff,
            IBackOff segmentsBackOff,
            int onDemandFetchMaxRetries,
            int onDemandFetchRetryDelayMs,
            ISegmentCacheConsumer segmentCache,
            IUniqueKeysTracker uniqueKeysTracker)
        {
            _splitFetcher = splitFetcher;
            _segmentFetcher = segmentFetcher;
            _impressionsLog = impressionsLog;
            _eventsLog = eventsLog;
            _impressionsCounter = impressionsCounter;            
            _wrapperAdapter = wrapperAdapter;
            _telemetrySyncTask = telemetrySyncTask;
            _featureFlagCacheConsumer = featureFlagCacheConsumer;
            _splitsBackOff = splitsBackOff;
            _segmentsBackOff = segmentsBackOff;
            _onDemandFetchMaxRetries = onDemandFetchMaxRetries;
            _onDemandFetchRetryDelayMs = onDemandFetchRetryDelayMs;
            _segmentCacheConsumer = segmentCache;
            _uniqueKeysTracker = uniqueKeysTracker;
            _defaultFetchOptions = new FetchOptions();
        }

        #region Public Methods
        public void StartPeriodicDataRecording()
        {
            _telemetrySyncTask.Start();
            _impressionsLog.Start();
            _eventsLog.Start();
            _impressionsCounter.Start();
            _uniqueKeysTracker.Start();
            _log.Debug("Periodic Data Recording started...");
        }

        public void StartPeriodicFetching()
        {
            _splitFetcher.Start();
            _segmentFetcher.Start();
            _log.Debug("Spltis and Segments fetchers started...");
        }

        public void StopPeriodicDataRecording()
        {
            _telemetrySyncTask.Stop();
            _impressionsLog.Stop();
            _eventsLog.Stop();
            _impressionsCounter.Stop();
            _uniqueKeysTracker.Stop();
            _log.Debug("Periodic Data Recording stopped...");
        }

        public void StopPeriodicFetching()
        {
            _splitFetcher.Stop();
            _segmentFetcher.Stop();
            _log.Debug("Spltis and Segments fetchers stopped...");
        }

        public void ClearFetchersCache()
        {
            _splitFetcher.Clear();
            _segmentFetcher.Clear();
        }

        public async Task<bool> SyncAllAsync()
        {
            var splitsResult = await _splitFetcher.FetchSplits(_defaultFetchOptions);
            var segmentsResult = _segmentFetcher.FetchAll();

            return splitsResult.Success && segmentsResult;
        }

        public async Task SynchronizeSegment(string segmentName, long targetChangeNumber)
        {
            try
            {
                if (targetChangeNumber <= _segmentCacheConsumer.GetChangeNumber(segmentName)) return;

                var fetchOptions = new FetchOptions { CacheControlHeaders = true };

                var result = await AttempSegmentSync(segmentName, targetChangeNumber, fetchOptions, _onDemandFetchMaxRetries, _onDemandFetchRetryDelayMs, false);

                if (result.Success)
                {
                    _log.Debug($"Segment {segmentName} refresh completed in {_onDemandFetchMaxRetries - result.RemainingAttempts} attempts.");

                    return;
                }

                fetchOptions.Till = targetChangeNumber;

                var withCDNBypassed = await AttempSegmentSync(segmentName, targetChangeNumber, fetchOptions, OnDemandFetchBackoffMaxRetries, null, true);

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

        public async Task SynchronizeSplits(long targetChangeNumber)
        {
            try
            {
                if (targetChangeNumber <= _featureFlagCacheConsumer.GetChangeNumber()) return;

                var fetchOptions = new FetchOptions { CacheControlHeaders = true };

                var result = await AttempSplitsSync(targetChangeNumber, fetchOptions, _onDemandFetchMaxRetries, _onDemandFetchRetryDelayMs, false);

                if (result.Success)
                {
                    await _segmentFetcher.FetchSegmentsIfNotExists(result.SegmentNames);
                    _log.Debug($"Refresh completed in {_onDemandFetchMaxRetries - result.RemainingAttempts} attempts.");

                    return;
                }

                fetchOptions.Till = targetChangeNumber;
                var withCDNBypassed = await AttempSplitsSync(targetChangeNumber, fetchOptions, OnDemandFetchBackoffMaxRetries, null, true);

                if (withCDNBypassed.Success)
                {
                    await _segmentFetcher.FetchSegmentsIfNotExists(withCDNBypassed.SegmentNames);
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
        private async Task<SyncResult> AttempSegmentSync(string name, long targetChangeNumber, FetchOptions fetchOptions, int maxRetries, int? retryDelayMs, bool withBackoff)
        {
            try
            {
                var remainingAttempts = maxRetries;

                if (withBackoff) _segmentsBackOff.Reset();

                while (true)
                {
                    remainingAttempts--;
                    await _segmentFetcher.Fetch(name, fetchOptions);

                    if (targetChangeNumber <= _segmentCacheConsumer.GetChangeNumber(name))
                    {
                        return new SyncResult(true, remainingAttempts);
                    }
                    else if (remainingAttempts <= 0)
                    {
                        return new SyncResult(false, remainingAttempts);
                    }

                    var delay = withBackoff ? _segmentsBackOff.GetInterval(inMiliseconds: true) : retryDelayMs.Value;
                    _wrapperAdapter.TaskDelay((int)delay).Wait();
                }
            }
            catch (Exception ex)
            {
                _log.Debug("Exception while AttempSegmentSync.", ex);
            }

            return new SyncResult(false, 0);
        }

        private async Task<SyncResult> AttempSplitsSync(long targetChangeNumber, FetchOptions fetchOptions, int maxRetries, int? retryDelayMs, bool withBackoff)
        {
            try
            {
                var remainingAttempts = maxRetries;

                if (withBackoff) _splitsBackOff.Reset();

                while (true)
                {
                    remainingAttempts--;
                    var result = await _splitFetcher.FetchSplits(fetchOptions);

                    if (targetChangeNumber <= _featureFlagCacheConsumer.GetChangeNumber())
                    {
                        return new SyncResult(true, remainingAttempts, result.SegmentNames);
                    }
                    else if (remainingAttempts <= 0)
                    {
                        return new SyncResult(false, remainingAttempts, result.SegmentNames);
                    }

                    var delay = withBackoff ? _splitsBackOff.GetInterval(inMiliseconds: true) : retryDelayMs.Value;
                    _wrapperAdapter.TaskDelay((int)delay).Wait();
                }
            }
            catch (Exception ex)
            {
                _log.Debug("Exception while AttempSplitsSync.", ex);
            }

            return new SyncResult(false, 0);
        }
        #endregion
    }
}
