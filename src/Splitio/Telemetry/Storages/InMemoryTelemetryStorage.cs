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
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(InMemoryTelemetryStorage));

        // Latencies
        private readonly ConcurrentDictionary<MethodEnum, long[]> _methodLatencies = new ConcurrentDictionary<MethodEnum, long[]>();
        private readonly ConcurrentDictionary<ResourceEnum, long[]> _httpLatencies = new ConcurrentDictionary<ResourceEnum, long[]>();
        private readonly object _methodLatenciesLock = new object();
        private readonly object _httpLatenciesLock = new object();

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
        private readonly ConcurrentDictionary<string, string> _tags = new ConcurrentDictionary<string, string>();

        // Streaming Events
        private readonly ConcurrentDictionary<string, StreamingEvent> _streamingEvents = new ConcurrentDictionary<string, StreamingEvent>();

        #region Public Methods - Producer
        public void AddTag(string tag)
        {
            try
            {
                if (_tags.Count < 10)
                {
                    _tags.TryAdd(tag, tag);
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
            lock (_methodLatencies)
            {
                try
                {
                    if (_methodLatencies.TryGetValue(method, out long[] values))
                    {
                        values[bucket]++;
                        return;
                    }

                    var latencies = new long[Util.Metrics.Buckets.Length];
                    latencies[bucket]++;

                    _methodLatencies.TryAdd(method, latencies);
                }
                catch (Exception ex)
                {
                    _log.Warn("Exception caught executing RecordLatency", ex);
                }
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
                    _streamingEvents.TryAdd(streamingEvent.ToString(), streamingEvent);
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
            lock (_httpLatenciesLock)
            {
                try
                {
                    if (_httpLatencies.TryGetValue(resource, out long[] value))
                    {
                        value[bucket]++;
                        return;
                    }

                    var latencies = new long[Util.Metrics.Buckets.Length];
                    latencies[bucket]++;

                    _httpLatencies.TryAdd(resource, latencies);
                }
                catch (Exception ex)
                {
                    _log.Warn("Exception caught executing RecordSyncLatency", ex);
                }
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
            _pushCounters.TryRemove(PushCountersEnum.AuthRejecttions, out long authRejections);

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
            var errors = new HTTPErrors
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

            return errors;
        }

        public HTTPLatencies PopHttpLatencies()
        {
            lock (_httpLatenciesLock)
            {
                var latencies = new HTTPLatencies
                {
                    Events = _httpLatencies.TryGetValue(ResourceEnum.EventSync, out long[] events) ? events.ToList() : new List<long>(),
                    Impressions = _httpLatencies.TryGetValue(ResourceEnum.ImpressionSync, out long[] impressions) ? impressions.ToList() : new List<long>(),
                    ImpressionCount = _httpLatencies.TryGetValue(ResourceEnum.ImpressionCountSync, out long[] impCounts) ? impCounts.ToList() : new List<long>(),
                    Segments = _httpLatencies.TryGetValue(ResourceEnum.SegmentSync, out long[] segments) ? segments.ToList() : new List<long>(),
                    Splits = _httpLatencies.TryGetValue(ResourceEnum.SplitSync, out long[] splits) ? splits.ToList() : new List<long>(),
                    Telemetry = _httpLatencies.TryGetValue(ResourceEnum.TelemetrySync, out long[] telemetries) ? telemetries.ToList() : new List<long>(),
                    Token = _httpLatencies.TryGetValue(ResourceEnum.TokenSync, out long[] tokens) ? tokens.ToList() : new List<long>()
                };

                _httpLatencies.Clear();

                return latencies;
            }
        }

        public MethodLatencies PopLatencies()
        {
            lock (_methodLatencies)
            {
                var latencies = new MethodLatencies
                {
                    Treatment = _methodLatencies.TryGetValue(MethodEnum.Treatment, out long[] treatment) ? treatment.ToList() : new List<long>(),
                    Treatments = _methodLatencies.TryGetValue(MethodEnum.Treatments, out long[] treatments) ? treatments.ToList() : new List<long>(),
                    TreatmenstWithConfig = _methodLatencies.TryGetValue(MethodEnum.TreatmentsWithConfig, out long[] treatmentsConfig) ? treatmentsConfig.ToList() : new List<long>(),
                    TreatmentWithConfig = _methodLatencies.TryGetValue(MethodEnum.TreatmentWithConfig, out long[] treatmentConfig) ? treatmentConfig.ToList() : new List<long>(),
                    Track = _methodLatencies.TryGetValue(MethodEnum.Track, out long[] track) ? track.ToList() : new List<long>()
                };

                _methodLatencies.Clear();

                return latencies;
            }
        }

        public IList<StreamingEvent> PopStreamingEvents()
        {
            var events = _streamingEvents.Values.ToList();
            _streamingEvents.Clear();

            return events;
        }

        public IList<string> PopTags()
        {
            var tags = _tags.Values.ToList();
            _tags.Clear();

            return tags;
        }

        public long PopTokenRefreshes()
        {
            _pushCounters.TryRemove(PushCountersEnum.TokenRefreshes, out long tokenRefreshes);

            return tokenRefreshes;
        }
        #endregion
    }
}
