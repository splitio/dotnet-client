using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
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
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly ISplitLogger _log;
        private readonly IImpressionsCountSender _impressionsCountSender;

        public Synchronizer(ISplitFetcher splitFetcher,
            ISelfRefreshingSegmentFetcher segmentFetcher,
            IImpressionsLog impressionsLog,
            IEventsLog eventsLog,
            IImpressionsCountSender impressionsCountSender,
            IWrapperAdapter wrapperAdapter = null,
            ISplitLogger log = null)
        {
            _splitFetcher = splitFetcher;
            _segmentFetcher = segmentFetcher;
            _impressionsLog = impressionsLog;
            _eventsLog = eventsLog;
            _impressionsCountSender = impressionsCountSender;
            _wrapperAdapter = wrapperAdapter ?? new WrapperAdapter();
            _log = log ?? WrapperAdapter.GetLogger(typeof(Synchronizer));
        }

        #region Public Methods
        public void StartPeriodicDataRecording()
        {
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
            var segmentNames = await _splitFetcher.FetchSplits();
            await _segmentFetcher.FetchSegmentsIfNotExists(segmentNames);
            _log.Debug("Splits fetched...");
        }
        #endregion
    }
}
