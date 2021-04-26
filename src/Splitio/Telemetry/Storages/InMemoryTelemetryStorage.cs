using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Telemetry.Storages
{
    public class InMemoryTelemetryStorage : ITelemetryStorageProducer, ITelemetryStorageConsumer
    {
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(InMemoryTelemetryStorage));

        // Latencies
        private readonly ConcurrentDictionary<MethodEnum, ConcurrentQueue<long>> _methodLatencies = new ConcurrentDictionary<MethodEnum, ConcurrentQueue<long>>();
        private readonly ConcurrentDictionary<ResourceEnum, ConcurrentQueue<long>> _httpLatencies = new ConcurrentDictionary<ResourceEnum, ConcurrentQueue<long>>();

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
        private ConcurrentQueue<string> _tags = new ConcurrentQueue<string>();

        // Streaming Events
        private ConcurrentQueue<StreamingEvent> _streamingEvents = new ConcurrentQueue<StreamingEvent>();

        #region Public Methods - Producer
        public void AddTag(string tag)
        {
            try
            {
                if (_tags.Count < 10)
                {
                    _tags.Enqueue(tag);
                }
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing AddTag", ex);
            }
        }

        public void RecordAuthRejections()
        {
            try
            { 
                _pushCounters.AddOrUpdate(PushCountersEnum.AuthRejecttions, 1, (key, value) => value + 1);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordAuthRejections", ex);
            }
        }

        public void RecordBURTimeout()
        {
            try
            {
                _factoryCounters.AddOrUpdate(FactoryCountersEnum.BurTimeouts, 1, (key, value) => value + 1);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordBURTimeout", ex);
            }
        }

        public void RecordEventsStats(EventsEnum data, long count)
        {
            try
            { 
                if (count <= 0) return;

                _eventsDataRecords.AddOrUpdate(data, count, (key, value) => value + count);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordEventsStats", ex);
            }
        }

        public void RecordException(MethodEnum method)
        {
            try
            { 
                _exceptionsCounters.AddOrUpdate(method, 1, (key, count) => count + 1);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordException", ex);
            }
        }

        public void RecordImpressionsStats(ImpressionsEnum data, long count)
        {
            try
            {
                if (count <= 0) return;

                _impressionsDataRecords.AddOrUpdate(data, count, (key, value) => value + count);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordImpressionsStats", ex);
            }
        }

        public void RecordLatency(MethodEnum method, int bucket)
        {
            try
            {
                if (_methodLatencies.TryGetValue(method, out ConcurrentQueue<long> queue))
                {
                    queue.Enqueue(bucket);
                    return;
                }

                var latencies = new ConcurrentQueue<long>();
                latencies.Enqueue(bucket);

                _methodLatencies.TryAdd(method, latencies);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordLatency", ex);
            }
        }

        public void RecordNonReadyUsages()
        {
            try
            { 
                _factoryCounters.AddOrUpdate(FactoryCountersEnum.NonReadyUsages, 1, (key, value) => value + 1);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordNonReadyUsages", ex);
            }
        }

        public void RecordSessionLength(long session)
        {
            try
            { 
                _sdkRecords.AddOrUpdate(SdkRecordsEnum.Session, session, (key, value) => session);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordSessionLength", ex);
            }
        }

        public void RecordStreamingEvent(StreamingEvent streamingEvent)
        {
            try
            {
                if (_streamingEvents.Count < 20)
                {
                    _streamingEvents.Enqueue(streamingEvent);
                }
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordStreamingEvent", ex);
            }
        }

        public void RecordSuccessfulSync(ResourceEnum resource, long timestamp)
        {
            try
            {
                _lastSynchronizationRecords.AddOrUpdate(resource, timestamp, (key, value) => timestamp);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordSuccessfulSync", ex);
            }
        }

        public void RecordSyncError(ResourceEnum resource, int status)
        {
            try
            {
                if (_httpErrors.TryGetValue(resource, out ConcurrentDictionary<int, long> value))
                {
                    value.AddOrUpdate(status, 1, (key, count) => count + 1);
                    return;
                }

                var errors = new ConcurrentDictionary<int, long>();
                errors.TryAdd(status, 1);
                _httpErrors.TryAdd(resource, errors);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordSyncError", ex);
            }
        }

        public void RecordSyncLatency(ResourceEnum resource, int bucket)
        {
            try
            {
                if (_httpLatencies.TryGetValue(resource, out ConcurrentQueue<long> queue))
                {
                    queue.Enqueue(bucket);
                    return;
                }

                var latencies = new ConcurrentQueue<long>();
                latencies.Enqueue(bucket);

                _httpLatencies.TryAdd(resource, latencies);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordSyncLatency", ex);
            }
        }

        public void RecordTokenRefreshes()
        {
            try
            {
                _pushCounters.AddOrUpdate(PushCountersEnum.TokenRefreshes, 1, (key, value) => value + 1);
            }
            catch (Exception ex)
            {
                _log.Warn("Exception caught executing RecordTokenRefreshes", ex);
            }
        }

        public void RecordConfigInit(Config config)
        {
            // No-Op. Config Data will be sent directly to Split Servers. No need to store.
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
                Impressions = _lastSynchronizationRecords.TryGetValue(ResourceEnum.ImpressionSync, out long impValue) ? impValue : 0,
                ImpressionCount = _lastSynchronizationRecords.TryGetValue(ResourceEnum.ImpressionCountSync, out long impCountValue) ? impCountValue : 0,
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
                Events = _httpErrors.TryGetValue(ResourceEnum.EventSync, out ConcurrentDictionary<int, long> events) ? events.ToDictionary(e => e.Key, e => e.Value) : new Dictionary<int, long>(),
                Impressions = _httpErrors.TryGetValue(ResourceEnum.ImpressionSync, out ConcurrentDictionary<int, long> impressions) ? impressions.ToDictionary(e => e.Key, e => e.Value) : new Dictionary<int, long>(),
                ImpressionCount = _httpErrors.TryGetValue(ResourceEnum.ImpressionCountSync, out ConcurrentDictionary<int, long> impCounts) ? impCounts.ToDictionary(e => e.Key, e => e.Value) : new Dictionary<int, long>(),
                Segments = _httpErrors.TryGetValue(ResourceEnum.SegmentSync, out ConcurrentDictionary<int, long> segments) ? segments.ToDictionary(e => e.Key, e => e.Value) : new Dictionary<int, long>(),
                Splits = _httpErrors.TryGetValue(ResourceEnum.SplitSync, out ConcurrentDictionary<int, long> splits) ? splits.ToDictionary(e => e.Key, e => e.Value) : new Dictionary<int, long>(),
                Telemetry = _httpErrors.TryGetValue(ResourceEnum.TelemetrySync, out ConcurrentDictionary<int, long> telemetries) ? telemetries.ToDictionary(e => e.Key, e => e.Value) : new Dictionary<int, long>(),
                Token = _httpErrors.TryGetValue(ResourceEnum.TokenSync, out ConcurrentDictionary<int, long> token) ? token.ToDictionary(e => e.Key, e => e.Value) : new Dictionary<int, long>()
            };

            _httpErrors.Clear();

            return erros;
        }

        public HTTPLatencies PopHttpLatencies()
        {
            var latencies = new HTTPLatencies
            {
                Events = _httpLatencies.TryGetValue(ResourceEnum.EventSync, out ConcurrentQueue<long> events) ? events.ToList() : new List<long>(),
                Impressions = _httpLatencies.TryGetValue(ResourceEnum.ImpressionSync, out ConcurrentQueue<long> impressions) ? impressions.ToList() : new List<long>(),
                ImpressionCount = _httpLatencies.TryGetValue(ResourceEnum.ImpressionCountSync, out ConcurrentQueue<long> impCounts) ? impCounts.ToList() : new List<long>(),
                Segments = _httpLatencies.TryGetValue(ResourceEnum.SegmentSync, out ConcurrentQueue<long> segments) ? segments.ToList() : new List<long>(),
                Splits = _httpLatencies.TryGetValue(ResourceEnum.SplitSync, out ConcurrentQueue<long> splits) ? splits.ToList() : new List<long>(),
                Telemetry = _httpLatencies.TryGetValue(ResourceEnum.TelemetrySync, out ConcurrentQueue<long> telemetries) ? telemetries.ToList() : new List<long>(),
                Token = _httpLatencies.TryGetValue(ResourceEnum.TokenSync, out ConcurrentQueue<long> tokens) ? tokens.ToList() : new List<long>()
            };            

            _httpLatencies.Clear();

            return latencies;
        }

        public MethodLatencies PopLatencies()
        {
            var latencies = new MethodLatencies
            {
                Treatment = _methodLatencies.TryGetValue(MethodEnum.Treatment, out ConcurrentQueue<long> treatment) ? treatment.ToList() : new List<long>(),
                Treatments = _methodLatencies.TryGetValue(MethodEnum.Treatments, out ConcurrentQueue<long> treatments) ? treatments.ToList() : new List<long>(),
                TreatmenstWithConfig = _methodLatencies.TryGetValue(MethodEnum.TreatmentsWithConfig, out ConcurrentQueue<long> treatmentsConfig) ? treatmentsConfig.ToList() : new List<long>(),
                TreatmentWithConfig = _methodLatencies.TryGetValue(MethodEnum.TreatmentWithConfig, out ConcurrentQueue<long> treatmentConfig) ? treatmentConfig.ToList() : new List<long>(),
                Track = _methodLatencies.TryGetValue(MethodEnum.Track, out ConcurrentQueue<long> track) ? track.ToList() : new List<long>()
            };

            _methodLatencies.Clear();

            return latencies;
        }

        public IList<StreamingEvent> PopStreamingEvents()
        {
            var events = new List<StreamingEvent>(_streamingEvents);
            _streamingEvents = new ConcurrentQueue<StreamingEvent>();

            return events;
        }

        public IList<string> PopTags()
        {
            var tags = new List<string>(_tags);
            _tags = new ConcurrentQueue<string>();

            return tags;
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
    }
}
