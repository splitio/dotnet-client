using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Telemetry.Storages
{
    public class InMemoryTelemetryStorage : ITelemetryStorage
    {
        // Latencies
        private readonly ConcurrentDictionary<MethodEnum, IList<long>> _methodLatencies = new ConcurrentDictionary<MethodEnum, IList<long>>();
        private readonly ConcurrentDictionary<HttpLatenciesEnum, IList<long>> _httpLatencies = new ConcurrentDictionary<HttpLatenciesEnum, IList<long>>();

        // Counters
        private readonly ConcurrentDictionary<MethodEnum, long> _exceptionsCounters = new ConcurrentDictionary<MethodEnum, long>();
        private readonly ConcurrentDictionary<PushCountersEnum, long> _pushCounters = new ConcurrentDictionary<PushCountersEnum, long>();
        private readonly ConcurrentDictionary<FactoryCountersEnum, long> _factoryCounters = new ConcurrentDictionary<FactoryCountersEnum, long>();

        // Records
        private readonly ConcurrentDictionary<ImpressionsDataRecordsEnum, long> _impressionsDataRecords = new ConcurrentDictionary<ImpressionsDataRecordsEnum, long>();
        private readonly ConcurrentDictionary<EventsDataRecordsEnum, long> _eventsDataRecords = new ConcurrentDictionary<EventsDataRecordsEnum, long>();
        private readonly ConcurrentDictionary<LastSynchronizationRecordsEnum, long> _lastSynchronizationRecords = new ConcurrentDictionary<LastSynchronizationRecordsEnum, long>();
        private readonly ConcurrentDictionary<SdkRecordsEnum, long> _sdkRecords = new ConcurrentDictionary<SdkRecordsEnum, long>();
        private readonly ConcurrentDictionary<FactoryRecordsEnum, long> _factoryRecords = new ConcurrentDictionary<FactoryRecordsEnum, long>();

        // HttpErrors
        private readonly ConcurrentDictionary<ResourceEnum, ConcurrentDictionary<long, long>> _httpErrors = new ConcurrentDictionary<ResourceEnum, ConcurrentDictionary<long, long>>();

        // Tags
        private readonly IList<string> _tags = new List<string>();
        private readonly object _tagsLock = new object();

        // Streaming Events
        private readonly IList<StreamingEvent> _streamingEvents = new List<StreamingEvent>();        
        private readonly object _streamingEventsLock = new object();

        public InMemoryTelemetryStorage()
        {
            InitLatencies();
            InitHttpLatencies();
            InitHttpErrors();
        }

        #region Public Methods - Producer
        public void AddTag(string tag)
        {
            lock (_tagsLock)
            {
                _tags.Add(tag);
            }
        }        

        public void RecordAuthRejections()
        {
            _pushCounters.AddOrUpdate(PushCountersEnum.AuthRejecttions, 1, (key, value) => value + 1);
        }

        public void RecordBURTimeout()
        {
            _factoryCounters.AddOrUpdate(FactoryCountersEnum.BurTimeouts, 1, (key, value) => value + 1);
        }

        public void RecordEventsStats(EventsDataRecordsEnum data, long count)
        {
            _eventsDataRecords.AddOrUpdate(data, count, (key, value) => value + count);
        }

        public void RecordException(MethodEnum method)
        {
            _exceptionsCounters.AddOrUpdate(method, 1, (key, count) => count + 1);
        }

        public void RecordImpressionsStats(ImpressionsDataRecordsEnum data, long count)
        {
            _impressionsDataRecords.AddOrUpdate(data, count, (key, value) => value + count);
        }

        public void RecordLatency(MethodEnum method, int bucket)
        {
            _methodLatencies[method].Add(bucket);
        }

        public void RecordNonReadyUsages()
        {
            _factoryCounters.AddOrUpdate(FactoryCountersEnum.NonReadyUsages, 1, (key, value) => value + 1);
        }

        public void RecordSessionLength(long session)
        {
            _sdkRecords.AddOrUpdate(SdkRecordsEnum.Session, session, (key, value) => session);
        }

        public void RecordStreamingEvent(StreamingEvent streamingEvent)
        {
            lock (_streamingEventsLock)
            {
                _streamingEvents.Add(streamingEvent);
            }
        }

        public void RecordSuccessfulSync(LastSynchronizationRecordsEnum resource, long timestamp)
        {
            _lastSynchronizationRecords.AddOrUpdate(resource, timestamp, (key, value) => timestamp);
        }

        public void RecordSyncError(ResourceEnum resource, int status)
        {
            _httpErrors[resource].AddOrUpdate(status, 1, (key, count) => count + 1);
        }

        public void RecordSyncLatency(HttpLatenciesEnum path, int bucket)
        {
            _httpLatencies[path].Add(bucket);
        }

        public void RecordTokenRefreshes()
        {
            _pushCounters.AddOrUpdate(PushCountersEnum.TokenRefreshes, 1, (key, value) => value + 1);
        }
        #endregion

        #region Public Methods - Consumer
        public long GetBURTimeouts()
        {
            return _factoryCounters[FactoryCountersEnum.BurTimeouts];
        }

        public long GetEventsStats(EventsDataRecordsEnum data)
        {
            return _eventsDataRecords[data];
        }

        public long GetImpressionsStats(ImpressionsDataRecordsEnum data)
        {
            return _impressionsDataRecords[data];
        }

        public LastSynchronization GetLastSynchronizations()
        {
            return new LastSynchronization
            {
                Splits = _lastSynchronizationRecords[LastSynchronizationRecordsEnum.Splits],
                Segments = _lastSynchronizationRecords[LastSynchronizationRecordsEnum.Segments],
                Impressions = _lastSynchronizationRecords[LastSynchronizationRecordsEnum.Impressions],
                Events = _lastSynchronizationRecords[LastSynchronizationRecordsEnum.Events],
                Telemetry = _lastSynchronizationRecords[LastSynchronizationRecordsEnum.Telemetry],
                Token = _lastSynchronizationRecords[LastSynchronizationRecordsEnum.Token]
            };
        }

        public long GetNonReadyUsages()
        {
            return _factoryCounters[FactoryCountersEnum.NonReadyUsages];
        }

        public long GetSessionLength()
        {
            return _sdkRecords[SdkRecordsEnum.Session];
        }

        public long PopAuthRejections()
        {
            var authRejections = _pushCounters[PushCountersEnum.AuthRejecttions];
            _pushCounters[PushCountersEnum.AuthRejecttions] = 0;

            return authRejections;
        }

        public MethodExceptions PopExceptions()
        {
            var exceptions = new MethodExceptions
            {
                Treatment = _exceptionsCounters[MethodEnum.Treatment],
                Treatments = _exceptionsCounters[MethodEnum.Treatments],
                TreatmentWithConfig = _exceptionsCounters[MethodEnum.TreatmentWithConfig],
                TreatmenstWithConfig = _exceptionsCounters[MethodEnum.TreatmentsWithConfig],
                Track = _exceptionsCounters[MethodEnum.Track],
            };

            _exceptionsCounters.Clear();

            return exceptions;
        }

        public HTTPErrors PopHttpErrors()
        {
            var erros = new HTTPErrors
            {
                Events = _httpErrors[ResourceEnum.EventSync],
                Impressions = _httpErrors[ResourceEnum.Impressionsync],
                Segments = _httpErrors[ResourceEnum.SegmentSync],
                Splits = _httpErrors[ResourceEnum.SplitSync],
                Telemetry = _httpErrors[ResourceEnum.TelemetrySync],
                Token = _httpErrors[ResourceEnum.TokenSync]
            };

            _httpErrors.Clear();

            return erros;
        }

        public HTTPLatencies PopHttpLatencies()
        {
            var latencies = new HTTPLatencies
            {
                Events = _httpLatencies[HttpLatenciesEnum.Events],
                Impressions = _httpLatencies[HttpLatenciesEnum.Impressions],
                Segments = _httpLatencies[HttpLatenciesEnum.Segments],
                Splits = _httpLatencies[HttpLatenciesEnum.Splits],
                Telemetry = _httpLatencies[HttpLatenciesEnum.Telemetry],
                Token = _httpLatencies[HttpLatenciesEnum.Token]
            };

            _httpLatencies.Clear();

            return latencies;
        }

        public MethodLatencies PopLatencies()
        {
            var latencies = new MethodLatencies
            {
                Treatment = _methodLatencies[MethodEnum.Treatment],
                Treatments = _methodLatencies[MethodEnum.Treatments],
                TreatmenstWithConfig = _methodLatencies[MethodEnum.TreatmentsWithConfig],
                TreatmentWithConfig = _methodLatencies[MethodEnum.TreatmentWithConfig],
                Track = _methodLatencies[MethodEnum.Track]
            };

            _methodLatencies.Clear();

            return latencies;
        }

        public IList<StreamingEvent> PopStreamingEvents()
        {
            lock (_streamingEventsLock)
            {
                var events = _streamingEvents;
                _streamingEvents.Clear();

                return events;
            }
        }

        public IList<string> PopTags()
        {
            lock (_tagsLock)
            {
                var tags = _tags;
                _tags.Clear();

                return tags;
            }
        }

        public long PopTokenRefreshes()
        {
            var tokenRefreshes = _pushCounters[PushCountersEnum.TokenRefreshes];
            _pushCounters[PushCountersEnum.TokenRefreshes] = 0;

            return tokenRefreshes;
        }
        #endregion

        #region Private Methods
        private void InitLatencies()
        {
            _methodLatencies.TryAdd(MethodEnum.Treatment, new List<long>());
            _methodLatencies.TryAdd(MethodEnum.Treatments, new List<long>());
            _methodLatencies.TryAdd(MethodEnum.TreatmentWithConfig, new List<long>());
            _methodLatencies.TryAdd(MethodEnum.TreatmentsWithConfig, new List<long>());
            _methodLatencies.TryAdd(MethodEnum.Track, new List<long>());
        }

        private void InitHttpLatencies()
        {
            _httpLatencies.TryAdd(HttpLatenciesEnum.Splits, new List<long>());
            _httpLatencies.TryAdd(HttpLatenciesEnum.Segments, new List<long>());
            _httpLatencies.TryAdd(HttpLatenciesEnum.Impressions, new List<long>());
            _httpLatencies.TryAdd(HttpLatenciesEnum.Events, new List<long>());
            _httpLatencies.TryAdd(HttpLatenciesEnum.Telemetry, new List<long>());
            _httpLatencies.TryAdd(HttpLatenciesEnum.Token, new List<long>());
        }

        private void InitHttpErrors()
        {
            _httpErrors.TryAdd(ResourceEnum.EventSync, new ConcurrentDictionary<long, long>());
            _httpErrors.TryAdd(ResourceEnum.Impressionsync, new ConcurrentDictionary<long, long>());
            _httpErrors.TryAdd(ResourceEnum.SegmentSync, new ConcurrentDictionary<long, long>());
            _httpErrors.TryAdd(ResourceEnum.SplitSync, new ConcurrentDictionary<long, long>());
            _httpErrors.TryAdd(ResourceEnum.TelemetrySync, new ConcurrentDictionary<long, long>());
            _httpErrors.TryAdd(ResourceEnum.TokenSync, new ConcurrentDictionary<long, long>());
        }
        #endregion
    }
}
