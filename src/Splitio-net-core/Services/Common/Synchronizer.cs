﻿using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Metrics.Interfaces;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.SplitFetcher.Interfaces;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class Synchronizer : ISynchronizer
    {
        private readonly ISplitFetcher _splitFetcher;
        private readonly ISelfRefreshingSegmentFetcher _segmentFetcher;
        private readonly IImpressionsLog _impressionsLog;
        private readonly IEventsLog _eventsLog;
        private readonly IMetricsLog _metricsLog;
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly ISplitLogger _log;
        private readonly IImpressionsCountSender _impressionsCountSender;

        public Synchronizer(ISplitFetcher splitFetcher,
            ISelfRefreshingSegmentFetcher segmentFetcher,
            IImpressionsLog impressionsLog,
            IEventsLog eventsLog,
            IMetricsLog metricsLog,
            IImpressionsCountSender impressionsCountSender,
            IWrapperAdapter wrapperAdapter = null,
            ISplitLogger log = null)
        {
            _splitFetcher = splitFetcher;
            _segmentFetcher = segmentFetcher;
            _impressionsLog = impressionsLog;
            _eventsLog = eventsLog;
            _metricsLog = metricsLog;
            _impressionsCountSender = impressionsCountSender;
            _wrapperAdapter = wrapperAdapter ?? new WrapperAdapter();
            _log = log ?? WrapperAdapter.GetLogger(typeof(Synchronizer));
        }

        #region Public Methods
        public void StartPeriodicDataRecording()
        {
            _impressionsLog.Start();
            _eventsLog.Start();
            _metricsLog.Start();
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
            _impressionsLog.Stop();
            _eventsLog.Stop();
            _metricsLog.Clear();
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

        public void SyncAll()
        {
            Task.Factory
                .StartNew(() => _splitFetcher.FetchSplits().Wait())
                .ContinueWith((x) => _segmentFetcher.FetchAll().Wait())
                .ContinueWith((x) => _log.Debug("Spltis and Segments synchronized..."));
        }

        public async Task SynchronizeSegment(string segmentName)
        {
            await _segmentFetcher.Fetch(segmentName);
            _log.Debug($"Segment fetched: {segmentName}...");
        }

        public async Task SynchronizeSplits()
        {
            await _splitFetcher.FetchSplits();
            _log.Debug("Splits fetched...");
        }
        #endregion
    }
}
