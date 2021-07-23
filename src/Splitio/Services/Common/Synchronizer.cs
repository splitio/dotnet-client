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
        private readonly IReadinessGatesCache _gates;
        private readonly ITelemetrySyncTask _telemetrySyncTask;
        private readonly ITasksManager _tasksManager;
        private readonly ISplitCache _splitCache;
        private readonly IBackOff _backOff;
        private readonly int _onDemandFetchMaxRetries;
        private readonly int _onDemandFetchRetryDelayMs;

        public Synchronizer(ISplitFetcher splitFetcher,
            ISelfRefreshingSegmentFetcher segmentFetcher,
            IImpressionsLog impressionsLog,
            IEventsLog eventsLog,
            IImpressionsCountSender impressionsCountSender,
            IWrapperAdapter wrapperAdapter,
            IReadinessGatesCache gates,
            ITelemetrySyncTask telemetrySyncTask,
            ITasksManager tasksManager,
            ISplitCache splitCache,
            IBackOff backOff,
            int onDemandFetchMaxRetries,
            int onDemandFetchRetryDelayMs,
            ISplitLogger log = null)
        {
            _splitFetcher = splitFetcher;
            _segmentFetcher = segmentFetcher;
            _impressionsLog = impressionsLog;
            _eventsLog = eventsLog;
            _impressionsCountSender = impressionsCountSender;            
            _wrapperAdapter = wrapperAdapter;
            _gates = gates;
            _telemetrySyncTask = telemetrySyncTask;
            _tasksManager = tasksManager;
            _splitCache = splitCache;
            _backOff = backOff;
            _onDemandFetchMaxRetries = onDemandFetchMaxRetries;
            _onDemandFetchRetryDelayMs = onDemandFetchRetryDelayMs;
            _log = log ?? WrapperAdapter.GetLogger(typeof(Synchronizer));
        }

        #region Public Methods
        public void StartPeriodicDataRecording()
        {
            _telemetrySyncTask.Start();
            _impressionsLog.Start();
            _eventsLog.Start();
            _impressionsCountSender.Start();
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

        public void SyncAll(CancellationTokenSource cancellationTokenSource)
        {
            _tasksManager.Start(() =>
            {
                _splitFetcher.FetchSplits(new FetchOptions()).Wait();
                _segmentFetcher.FetchAll().Wait();
                _gates.SdkInternalReady();
                _log.Debug("Spltis and Segments synchronized...");
            }, cancellationTokenSource, "SyncAll");
        }

        public async Task SynchronizeSegment(string segmentName)
        {
            await _segmentFetcher.Fetch(segmentName, new FetchOptions { CacheControlHeaders = true });
            _log.Debug($"Segment fetched: {segmentName}...");
        }

        public async Task SynchronizeSplits(long targetChangeNumber)
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
            result = await AttempSplitsSync(targetChangeNumber, fetchOptions, OnDemandFetchBackoffMaxRetries, null, true);

            if (result.Success)
            {
                await _segmentFetcher.FetchSegmentsIfNotExists(result.SegmentNames);
                _log.Debug($"Refresh completed bypassing the CDN in {OnDemandFetchBackoffMaxRetries - result.RemainingAttempts} attempts.");
            }
            else
            {
                _log.Debug($"No changes fetched after #{OnDemandFetchBackoffMaxRetries - result.RemainingAttempts} attempts with CDN bypassed.");
            }
        }
        #endregion

        #region Private Methods
        private async Task<SyncResult> AttempSplitsSync(long targetChangeNumber, FetchOptions fetchOptions, int maxRetries, int? retryDelayMs, bool withBackoff)
        {
            try
            {
                var remainingAttempts = maxRetries;

                if (withBackoff) _backOff.Reset();                

                while (true)
                {
                    remainingAttempts--;
                    var segmentNames = await _splitFetcher.FetchSplits(fetchOptions);

                    if (targetChangeNumber <= _splitCache.GetChangeNumber())
                    {
                        return new SyncResult(true, remainingAttempts, segmentNames);
                    }
                    else if (remainingAttempts <= 0)
                    {
                        return new SyncResult(false, remainingAttempts, segmentNames);
                    }

                    var delay = withBackoff ? _backOff.GetInterval(inMiliseconds: true) : retryDelayMs.Value;
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
