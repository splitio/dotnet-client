using Splitio.Domain;
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
using System.Threading;
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
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly ISplitLogger _log;
        private readonly IImpressionsCountSender _impressionsCountSender;
        private readonly IStatusManager _statusManager;
        private readonly ITelemetrySyncTask _telemetrySyncTask;
        private readonly ITasksManager _tasksManager;
        private readonly ISplitCache _splitCache;
        private readonly ISegmentCache _segmentCache;
        private readonly IBackOff _backOffSplits;
        private readonly IBackOff _backOffSegments;
        private readonly IUniqueKeysTracker _uniqueKeysTracker;
        private readonly int _onDemandFetchMaxRetries;
        private readonly int _onDemandFetchRetryDelayMs;
        private readonly FetchOptions _defaultFetchOptions;

        public Synchronizer(SynchronizerConfig config)
        {
            _splitFetcher = config.SplitFetcher;
            _segmentFetcher = config.SegmentFetcher;
            _impressionsLog = config.ImpressionsLog;
            _eventsLog = config.EventsLog;
            _impressionsCountSender = config.ImpressionsCountSender;
            _wrapperAdapter = config.WrapperAdapter;
            _statusManager = config.StatusManager;
            _telemetrySyncTask = config.TelemetrySyncTask;
            _tasksManager = config.TasksManager;
            _splitCache = config.SplitCache;
            _backOffSplits = config.BackOffSplits;
            _backOffSegments = config.BackOffSegments;
            _onDemandFetchMaxRetries = config.OnDemandFetchMaxRetries;
            _onDemandFetchRetryDelayMs = config.OnDemandFetchRetryDelayMs;
            _segmentCache = config.SegmentCache;
            _uniqueKeysTracker = config.UniqueKeysTracker;
            _log = config.Loggger ?? WrapperAdapter.GetLogger(typeof(Synchronizer));
            _defaultFetchOptions = new FetchOptions();
        }

        #region Public Methods
        public void StartPeriodicDataRecording()
        {
            _telemetrySyncTask.Start();
            _impressionsLog.Start();
            _eventsLog.Start();
            _impressionsCountSender.Start();
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
            _impressionsCountSender.Stop();
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

        public bool SyncAll(CancellationTokenSource cancellationTokenSource, bool asynchronous = true)
        {
            if (asynchronous)
            {
                _tasksManager.Start(() => SyncAll(), cancellationTokenSource, "SyncAll");
                return true;
            }

            return SyncAll();
        }

        public async Task SynchronizeSegment(string segmentName, long targetChangeNumber)
        {
            try
            {
                if (targetChangeNumber <= _segmentCache.GetChangeNumber(segmentName)) return;

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
                if (targetChangeNumber <= _splitCache.GetChangeNumber()) return;

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

                if (withBackoff) _backOffSplits.Reset();

                while (true)
                {
                    remainingAttempts--;
                    await _segmentFetcher.Fetch(name, fetchOptions);

                    if (targetChangeNumber <= _segmentCache.GetChangeNumber(name))
                    {
                        return new SyncResult(true, remainingAttempts);
                    }
                    else if (remainingAttempts <= 0)
                    {
                        return new SyncResult(false, remainingAttempts);
                    }

                    var delay = withBackoff ? _backOffSplits.GetInterval(inMiliseconds: true) : retryDelayMs.Value;
                    _wrapperAdapter.TaskDelay((int)delay).Wait();
                }
            }
            catch (Exception ex)
            {
                _log.Debug("Exception while AttempSplitsSync.", ex);
            }

            return new SyncResult(false, 0);
        }

        private async Task<SyncResult> AttempSplitsSync(long targetChangeNumber, FetchOptions fetchOptions, int maxRetries, int? retryDelayMs, bool withBackoff)
        {
            try
            {
                var remainingAttempts = maxRetries;

                if (withBackoff) _backOffSplits.Reset();

                while (true)
                {
                    remainingAttempts--;
                    var result = await _splitFetcher.FetchSplits(fetchOptions);

                    if (targetChangeNumber <= _splitCache.GetChangeNumber())
                    {
                        return new SyncResult(true, remainingAttempts, result.SegmentNames);
                    }
                    else if (remainingAttempts <= 0)
                    {
                        return new SyncResult(false, remainingAttempts, result.SegmentNames);
                    }

                    var delay = withBackoff ? _backOffSplits.GetInterval(inMiliseconds: true) : retryDelayMs.Value;
                    _wrapperAdapter.TaskDelay((int)delay).Wait();
                }
            }
            catch (Exception ex)
            {
                _log.Debug("Exception while AttempSplitsSync.", ex);
            }

            return new SyncResult(false, 0);
        }

        private bool SyncAll()
        {
            var splitsResult = _splitFetcher.FetchSplits(_defaultFetchOptions).Result;

            return splitsResult.Success && _segmentFetcher.FetchAll();
        }
        #endregion
    }

    public class SynchronizerConfig
    {
        public ISplitFetcher SplitFetcher { get; set; }
        public ISelfRefreshingSegmentFetcher SegmentFetcher { get; set; }
        public IImpressionsLog ImpressionsLog { get; set; }
        public IEventsLog EventsLog { get; set; }
        public IImpressionsCountSender ImpressionsCountSender { get; set; }
        public IWrapperAdapter WrapperAdapter { get; set; }
        public IStatusManager StatusManager { get; set; }
        public ITelemetrySyncTask TelemetrySyncTask { get; set; }
        public ITasksManager TasksManager { get; set; }
        public ISplitCache SplitCache { get; set; }
        public IBackOff BackOffSplits { get; set; }
        public IBackOff BackOffSegments { get; set; }
        public int OnDemandFetchMaxRetries { get; set; }
        public int OnDemandFetchRetryDelayMs { get; set; }
        public ISegmentCache SegmentCache { get; set; }
        public IUniqueKeysTracker UniqueKeysTracker { get; set; }
        public ISplitLogger Loggger { get; set; }
    }
}
