using Splitio.CommonLibraries;
using Splitio.Services.Cache.Interfaces;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Services.Interfaces;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;

namespace Splitio.Telemetry.Services
{
    public class TelemetryFacade : ITelemetryFacade
    {
        private readonly ITelemetryStorage _telemetryStorage;
        private readonly ISplitCache _splitCache;
        private readonly ISegmentCache _segmentCache;

        public TelemetryFacade(ITelemetryStorage telemetryStorage,
            ISplitCache splitCache,
            ISegmentCache segmentCache)
        {
            _telemetryStorage = telemetryStorage;
            _splitCache = splitCache;
            _segmentCache = segmentCache;
        }

        #region Public Methods - Producer
        public void RecordException(MethodEnum method)
        {
            _telemetryStorage.RecordException(method);
        }

        public void RecordLatency(MethodEnum method, long latency)
        {
            _telemetryStorage.RecordLatency(method, Util.Metrics.Bucket(latency));
        }

        public void RecordImpressionsStats(ImpressionsEnum data, long count)
        {
            _telemetryStorage.RecordImpressionsStats(data, count);
        }

        public void RecordEventsStats(EventsEnum data, long count)
        {
            _telemetryStorage.RecordEventsStats(data, count);
        }

        public void RecordSuccessfulSync(ResourceEnum resource)
        {
            _telemetryStorage.RecordSuccessfulSync(resource, CurrentTimeHelper.CurrentTimeMillis());
        }

        public void RecordSyncError(ResourceEnum resource, int status)
        {
            _telemetryStorage.RecordSyncError(resource, status);
        }

        public void RecordSyncLatency(ResourceEnum resource, long latency)
        {
            _telemetryStorage.RecordSyncLatency(resource, Util.Metrics.Bucket(latency));
        }

        public void RecordAuthRejections()
        {
            _telemetryStorage.RecordAuthRejections();
        }

        public void RecordTokenRefreshes()
        {
            _telemetryStorage.RecordTokenRefreshes();
        }

        public void RecordStreamingEvent(EventTypeEnum type, long data)
        {
            _telemetryStorage.RecordStreamingEvent(new StreamingEvent
            {
                Type = (int)type,
                Data = data,
                Timestamp = CurrentTimeHelper.CurrentTimeMillis()
            });
        }

        public void AddTag(string tag)
        {
            _telemetryStorage.AddTag(tag);
        }

        public void RecordSessionLength(long session)
        {
            _telemetryStorage.RecordSessionLength(session);
        }

        public void RecordBURTimeout()
        {
            _telemetryStorage.RecordBURTimeout();
        }

        public void RecordNonReadyUsages()
        {
            _telemetryStorage.RecordNonReadyUsages();
        }
        #endregion

        #region Public Methods - Consumer
        public MethodLatencies PopLatencies()
        {
            return _telemetryStorage.PopLatencies();
        }

        public MethodExceptions PopExceptions()
        {
            return _telemetryStorage.PopExceptions();
        }

        public long GetImpressionsStats(ImpressionsEnum data)
        {
            return _telemetryStorage.GetImpressionsStats(data);
        }

        public long GetEventsStats(EventsEnum data)
        {
            return _telemetryStorage.GetEventsStats(data);
        }

        public LastSynchronization GetLastSynchronizations()
        {
            return _telemetryStorage.GetLastSynchronizations();
        }

        public HTTPErrors PopHttpErrors()
        {
            return _telemetryStorage.PopHttpErrors();
        }

        public HTTPLatencies PopHttpLatencies()
        {
            return _telemetryStorage.PopHttpLatencies();
        }

        public long GetSplitsCount()
        {
            return _splitCache.GetSplitNames().Count;
        }

        public long GetSegmentsCount()
        {
            return _segmentCache.GetSegmentNames().Count;
        }

        public long GetSegmentKeysCount()
        {
            // Should implement segment cache method
            throw new System.NotImplementedException();
        }

        public long PopAuthRejections()
        {
            return _telemetryStorage.PopAuthRejections();
        }

        public long PopTokenRefreshes()
        {
            return _telemetryStorage.PopTokenRefreshes();
        }

        public IList<StreamingEvent> PopStreamingEvents()
        {
            return _telemetryStorage.PopStreamingEvents();
        }

        public IList<string> PopTags()
        {
            return _telemetryStorage.PopTags();
        }

        public long GetSessionLength()
        {
            return _telemetryStorage.GetSessionLength();
        }

        public long GetBURTimeouts()
        {
            return _telemetryStorage.GetBURTimeouts();
        }

        public long GetNonReadyUsages()
        {
            return _telemetryStorage.GetNonReadyUsages();
        }
        #endregion
    }
}
