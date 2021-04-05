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
        private readonly ConcurrentDictionary<ResourceEnum, IList<long>> _httpLatencies = new ConcurrentDictionary<ResourceEnum, IList<long>>();

        // Counters
        private readonly ConcurrentDictionary<MethodEnum, long> _exceptionsCounters = new ConcurrentDictionary<MethodEnum, long>();
        private readonly ConcurrentDictionary<PushCountersEnum, long> _pushCounters = new ConcurrentDictionary<PushCountersEnum, long>();
        private readonly ConcurrentDictionary<FactoryCountersEnum, long> _factoryCounters = new ConcurrentDictionary<FactoryCountersEnum, long>();

        // Records
        private readonly ConcurrentDictionary<ImpressionsEnum, long> _impressionsDataRecords = new ConcurrentDictionary<ImpressionsEnum, long>();
        private readonly ConcurrentDictionary<EventsEnum, long> _eventsDataRecords = new ConcurrentDictionary<EventsEnum, long>();
        private readonly ConcurrentDictionary<ResourceEnum, long> _lastSynchronizationRecords = new ConcurrentDictionary<ResourceEnum, long>();
        private readonly ConcurrentDictionary<SdkRecordsEnum, long> _sdkRecords = new ConcurrentDictionary<SdkRecordsEnum, long>();
        private readonly ConcurrentDictionary<FactoryRecordsEnum, long> _factoryRecords = new ConcurrentDictionary<FactoryRecordsEnum, long>();

        // HttpErrors
        private readonly ConcurrentDictionary<ResourceEnum, ConcurrentDictionary<int, long>> _httpErrors = new ConcurrentDictionary<ResourceEnum, ConcurrentDictionary<int, long>>();

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

        public void RecordEventsStats(EventsEnum data, long count)
        {
            _eventsDataRecords.AddOrUpdate(data, count, (key, value) => value + count);
        }

        public void RecordException(MethodEnum method)
        {
            _exceptionsCounters.AddOrUpdate(method, 1, (key, count) => count + 1);
        }

        public void RecordImpressionsStats(ImpressionsEnum data, long count)
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

        public void RecordSuccessfulSync(ResourceEnum resource, long timestamp)
        {
            _lastSynchronizationRecords.AddOrUpdate(resource, timestamp, (key, value) => timestamp);
        }

        public void RecordSyncError(ResourceEnum resource, int status)
        {
            _httpErrors[resource].AddOrUpdate(status, 1, (key, count) => count + 1);
        }

        public void RecordSyncLatency(ResourceEnum resource, int bucket)
        {
            _httpLatencies[resource].Add(bucket);
        }

        public void RecordTokenRefreshes()
        {
            _pushCounters.AddOrUpdate(PushCountersEnum.TokenRefreshes, 1, (key, value) => value + 1);
        }

        public void RecordInit()
        {
            
        }
        #endregion

        #region Public Methods - Consumer
        public long GetBURTimeouts()
        {
            if (!_factoryCounters.TryGetValue(FactoryCountersEnum.BurTimeouts, out long value))
            {
                return 0;
            }

            return value;
        }

        public long GetEventsStats(EventsEnum data)
        {
            return _eventsDataRecords.TryGetValue(data, out long value) ? value : 0;
        }

        public long GetImpressionsStats(ImpressionsEnum data)
        {            
            return _impressionsDataRecords.TryGetValue(data, out long value) ? value : 0;
        }

        public LastSynchronization GetLastSynchronizations()
        {
            return new LastSynchronization
            {
                Splits = _lastSynchronizationRecords.TryGetValue(ResourceEnum.SplitSync, out long splitsValue) ? splitsValue : 0,
                Segments = _lastSynchronizationRecords.TryGetValue(ResourceEnum.SegmentSync, out long segValue) ? segValue : 0,
                Impressions = _lastSynchronizationRecords.TryGetValue(ResourceEnum.Impressionsync, out long impValue) ? impValue : 0,
                Events = _lastSynchronizationRecords.TryGetValue(ResourceEnum.EventSync, out long evetsValue) ? evetsValue : 0,
                Telemetry = _lastSynchronizationRecords.TryGetValue(ResourceEnum.TelemetrySync, out long telValue) ? telValue : 0,
                Token = _lastSynchronizationRecords.TryGetValue(ResourceEnum.TokenSync, out long tokenValue) ? tokenValue : 0
            };
        }

        public long GetNonReadyUsages()
        {
            if (!_factoryCounters.TryGetValue(FactoryCountersEnum.NonReadyUsages, out long value))
            {
                return 0;
            }

            return value;
        }

        public long GetSessionLength()
        {
            return _sdkRecords.TryGetValue(SdkRecordsEnum.Session, out long value) ? value : 0;
        }

        public long PopAuthRejections()
        {
            if (!_pushCounters.TryGetValue(PushCountersEnum.AuthRejecttions, out long authRejections))
            {
                return 0;
            }

            _pushCounters[PushCountersEnum.AuthRejecttions] = 0;

            return authRejections;
        }

        public MethodExceptions PopExceptions()
        {
            var exceptions = new MethodExceptions
            {
                Treatment = _exceptionsCounters.TryGetValue(MethodEnum.Treatment, out long treatmentValue) ? treatmentValue : 0,
                Treatments = _exceptionsCounters.TryGetValue(MethodEnum.Treatments, out long treatmentsValue) ? treatmentsValue : 0,
                TreatmentWithConfig = _exceptionsCounters.TryGetValue(MethodEnum.TreatmentWithConfig, out long twcValue) ? twcValue : 0,
                TreatmentsWithConfig = _exceptionsCounters.TryGetValue(MethodEnum.TreatmentsWithConfig, out long tswcValue) ? tswcValue : 0,
                Track = _exceptionsCounters.TryGetValue(MethodEnum.Track, out long trackValue) ? trackValue : 0,
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
            InitHttpErrors();

            return erros;
        }

        public HTTPLatencies PopHttpLatencies()
        {
            var latencies = new HTTPLatencies
            {
                Events = _httpLatencies[ResourceEnum.EventSync],
                Impressions = _httpLatencies[ResourceEnum.Impressionsync],
                Segments = _httpLatencies[ResourceEnum.SegmentSync],
                Splits = _httpLatencies[ResourceEnum.SplitSync],
                Telemetry = _httpLatencies[ResourceEnum.TelemetrySync],
                Token = _httpLatencies[ResourceEnum.TokenSync]
            };

            _httpLatencies.Clear();
            InitHttpLatencies();

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
            InitLatencies();

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
            if (!_pushCounters.TryGetValue(PushCountersEnum.TokenRefreshes, out long tokenRefreshes))
            {
                return 0;
            }

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
            _httpLatencies.TryAdd(ResourceEnum.SplitSync, new List<long>());
            _httpLatencies.TryAdd(ResourceEnum.SegmentSync, new List<long>());
            _httpLatencies.TryAdd(ResourceEnum.Impressionsync, new List<long>());
            _httpLatencies.TryAdd(ResourceEnum.EventSync, new List<long>());
            _httpLatencies.TryAdd(ResourceEnum.TelemetrySync, new List<long>());
            _httpLatencies.TryAdd(ResourceEnum.TokenSync, new List<long>());
        }

        private void InitHttpErrors()
        {
            _httpErrors.TryAdd(ResourceEnum.EventSync, new ConcurrentDictionary<int, long>());
            _httpErrors.TryAdd(ResourceEnum.Impressionsync, new ConcurrentDictionary<int, long>());
            _httpErrors.TryAdd(ResourceEnum.SegmentSync, new ConcurrentDictionary<int, long>());
            _httpErrors.TryAdd(ResourceEnum.SplitSync, new ConcurrentDictionary<int, long>());
            _httpErrors.TryAdd(ResourceEnum.TelemetrySync, new ConcurrentDictionary<int, long>());
            _httpErrors.TryAdd(ResourceEnum.TokenSync, new ConcurrentDictionary<int, long>());
        }        
        #endregion
    }
}
