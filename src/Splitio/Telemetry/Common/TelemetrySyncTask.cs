using Splitio.CommonLibraries;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Linq;
using System.Threading;

namespace Splitio.Telemetry.Common
{
    public class TelemetrySyncTask : ITelemetrySyncTask
    {
        private readonly ITelemetryStorageConsumer _telemetryStorage;
        private readonly ITelemetryAPI _telemetryAPI;
        private readonly ISplitCache _splitCache;
        private readonly ISegmentCache _segmentCache;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ISplitLogger _log;
        private readonly int _refreshRate;

        public TelemetrySyncTask(ITelemetryStorageConsumer telemetryStorage,
            ITelemetryAPI telemetryAPI,            
            ISplitCache splitCache,
            ISegmentCache segmentCache,
            int refreshRate,
            ISplitLogger log = null)
        {
            _telemetryStorage = telemetryStorage;
            _telemetryAPI = telemetryAPI;            
            _splitCache = splitCache;
            _segmentCache = segmentCache;
            _refreshRate = refreshRate;
            _cancellationTokenSource = new CancellationTokenSource();
            _log = log ?? WrapperAdapter.GetLogger(typeof(TelemetrySyncTask));
        }

        #region Public Methods
        public void Start()
        {
            PeriodicTaskFactory.Start(() => { RecordStats(); }, _refreshRate * 1000, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            RecordStats();
        }
        #endregion

        #region Private Methods
        private void RecordStats()
        {
            try
            {
                var stats = new Stats
                {
                    AuthRejections = _telemetryStorage.PopAuthRejections(),
                    EventsDropped = _telemetryStorage.GetEventsStats(EventsEnum.EventsDropped),
                    EventsQueued = _telemetryStorage.GetEventsStats(EventsEnum.EventsQueued),
                    HTTPErrors = _telemetryStorage.PopHttpErrors(),
                    HTTPLatencies = _telemetryStorage.PopHttpLatencies(),
                    ImpressionsDeduped = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsDeduped),
                    ImpressionsDropped = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsDropped),
                    ImpressionsQueued = _telemetryStorage.GetImpressionsStats(ImpressionsEnum.ImpressionsQueued),
                    LastSynchronizations = _telemetryStorage.GetLastSynchronizations(),
                    MethodExceptions = _telemetryStorage.PopExceptions(),
                    MethodLatencies = _telemetryStorage.PopLatencies(),
                    SessionLengthMs = _telemetryStorage.GetSessionLength(),
                    StreamingEvents = _telemetryStorage.PopStreamingEvents().ToList(),
                    Tags = _telemetryStorage.PopTags().ToList(),
                    TokenRefreshes = _telemetryStorage.PopTokenRefreshes(),
                    SplitCount = _splitCache.SplitsCount(),
                    SegmentCount = _segmentCache.SegmentsCount(),
                    SegmentKeyCount = _segmentCache.SegmentKeysCount()
                };

                _telemetryAPI.RecordStats(stats);
            }
            catch (Exception ex)
            {
                _log.Error("Something were wrong posting Stats.", ex);
            }
        }
        #endregion
    }
}
