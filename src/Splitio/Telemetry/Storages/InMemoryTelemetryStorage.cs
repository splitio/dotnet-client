using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Telemetry.Storages
{
    public class InMemoryTelemetryStorage : ITelemetryStorageProducer, ITelemetryStorageConsumer
    {
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(InMemoryTelemetryStorage));

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
            try
            {
                lock (_tagsLock)
                {
                    if (_tags.Count < 10)
                    {
                        _tags.Add(tag);
                    }
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
                _methodLatencies[method].Add(bucket);
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
                lock (_streamingEventsLock)
                {
                    if (_streamingEvents.Count < 20)
                    {
                        _streamingEvents.Add(streamingEvent);
                    }
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
                _httpErrors[resource].AddOrUpdate(status, 1, (key, count) => count + 1);
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
                _httpLatencies[resource].Add(bucket);
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
                Events = _httpErrors[ResourceEnum.EventSync],
                Impressions = _httpErrors[ResourceEnum.ImpressionSync],
                ImpressionCount = _httpErrors[ResourceEnum.ImpressionCountSync],
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
                Impressions = _httpLatencies[ResourceEnum.ImpressionSync],
                ImpressionCount = _httpLatencies[ResourceEnum.ImpressionCountSync],
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
                var events = new List<StreamingEvent>(_streamingEvents);
                _streamingEvents.Clear();

                return events;
            }
        }

        public IList<string> PopTags()
        {
            lock (_tagsLock)
            {
                var tags = new List<string>(_tags);
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
            _httpLatencies.TryAdd(ResourceEnum.ImpressionSync, new List<long>());
            _httpLatencies.TryAdd(ResourceEnum.ImpressionCountSync, new List<long>());
            _httpLatencies.TryAdd(ResourceEnum.EventSync, new List<long>());
            _httpLatencies.TryAdd(ResourceEnum.TelemetrySync, new List<long>());
            _httpLatencies.TryAdd(ResourceEnum.TokenSync, new List<long>());
        }

        private void InitHttpErrors()
        {
            _httpErrors.TryAdd(ResourceEnum.EventSync, new ConcurrentDictionary<int, long>());
            _httpErrors.TryAdd(ResourceEnum.ImpressionSync, new ConcurrentDictionary<int, long>());
            _httpErrors.TryAdd(ResourceEnum.ImpressionCountSync, new ConcurrentDictionary<int, long>());
            _httpErrors.TryAdd(ResourceEnum.SegmentSync, new ConcurrentDictionary<int, long>());
            _httpErrors.TryAdd(ResourceEnum.SplitSync, new ConcurrentDictionary<int, long>());
            _httpErrors.TryAdd(ResourceEnum.TelemetrySync, new ConcurrentDictionary<int, long>());
            _httpErrors.TryAdd(ResourceEnum.TokenSync, new ConcurrentDictionary<int, long>());
        }        
        #endregion
    }
}
