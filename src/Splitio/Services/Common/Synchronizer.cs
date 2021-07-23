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
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class Synchronizer : ISynchronizer
    {
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

        public Synchronizer(ISplitFetcher splitFetcher,
            ISelfRefreshingSegmentFetcher segmentFetcher,
            IImpressionsLog impressionsLog,
            IEventsLog eventsLog,
            IImpressionsCountSender impressionsCountSender,
            IWrapperAdapter wrapperAdapter,
            IReadinessGatesCache gates,
            ITelemetrySyncTask telemetrySyncTask,
            ITasksManager tasksManager,
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

        public async Task SynchronizeSplits()
        {
            var segmentNames = await _splitFetcher.FetchSplits(new FetchOptions { CacheControlHeaders = true });
            await _segmentFetcher.FetchSegmentsIfNotExists(segmentNames);
            _log.Debug("Splits fetched...");
        }
        #endregion
    }
}
