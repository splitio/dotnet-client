using Splitio.Telemetry.Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Telemetry.Storages
{
    public class InMemoryTelemetryStorage : ITelemetryStorage
    {
        private readonly ConcurrentDictionary<MethodEnum, IList<long>> _latencies = new ConcurrentDictionary<MethodEnum, IList<long>>();
        private readonly ConcurrentDictionary<MethodEnum, long> _counters = new ConcurrentDictionary<MethodEnum, long>();
        private readonly ConcurrentDictionary<RecordsEnum, long> _records = new ConcurrentDictionary<RecordsEnum, long>();
        private readonly ConcurrentDictionary<ResourceEnum, ConcurrentDictionary<long, long>> _httpErrors = new ConcurrentDictionary<ResourceEnum, ConcurrentDictionary<long, long>>();
        private readonly IList<string> _tags = new List<string>();
        private readonly IList<StreamingEvent> _streamingEvents = new List<StreamingEvent>();

        private readonly object _tagsLock = new object();
        private readonly object _streamingEventsLock = new object();

        public InMemoryTelemetryStorage()
        {
            InitLatencies();
            InitHttpErrors();
        }

        public void AddTag(string tag)
        {
            lock (_tagsLock)
            {
                _tags.Add(tag);
            }
        }

        public long GetBURTimeouts()
        {
            
            //_counters.AddOrUpdate(MethodEnum., 1, (key, count) => count + 1);
            throw new NotImplementedException();
        }

        public long GetEventsStats(RecordsEnum data)
        {
            return _records[data];
        }

        public long GetImpressionsStats(RecordsEnum data)
        {
            return _records[data];
        }

        public LastSynchronization GetLAstSynchronizations()
        {
            return new LastSynchronization
            {
                Splits = _records[RecordsEnum.Splits],
                Segments = _records[RecordsEnum.Segments],
                Impressions = _records[RecordsEnum.Impressions],
                Events = _records[RecordsEnum.Events],
                Telemetry = _records[RecordsEnum.Telemetry],
                Token = _records[RecordsEnum.Token]
            };
        }

        public long GetNonReadyUsages()
        {
            throw new NotImplementedException();
        }

        public long GetSessionLength()
        {
            throw new NotImplementedException();
        }

        public long PopAuthRejections()
        {
            throw new NotImplementedException();
        }

        public IDictionary<MethodEnum, long> PopExceptions()
        {
            var exceptions = new Dictionary<MethodEnum, long>(_counters);
            _counters.Clear();

            return exceptions;
        }

        public HTTPErrors PopHttpErrors()
        {
            var erros = new HTTPErrors
            {
                Events =  _httpErrors[ResourceEnum.EventSync],
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
            throw new NotImplementedException();
        }

        public MethodLatencies PopLatencies()
        {
            var latencies = new MethodLatencies
            {
                Treatment = _latencies[MethodEnum.Treatment],
                Treatments = _latencies[MethodEnum.Treatments],                
                TreatmenstWithConfig = _latencies[MethodEnum.TreatmentsWithConfig],                
                TreatmentWithConfig = _latencies[MethodEnum.TreatmentWithConfig],
                Track = _latencies[MethodEnum.Track]
            };

            _latencies.Clear();

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
            throw new NotImplementedException();
        }

        public void RecordAuthRejections()
        {
            throw new NotImplementedException();
        }

        public void RecordBURTimeout()
        {
            throw new NotImplementedException();
        }

        public void RecordEventsStats(RecordsEnum data, long count)
        {
            _records.AddOrUpdate(data, count, (key, value) => value + count);
        }

        public void RecordException(MethodEnum method)
        {
            _counters.AddOrUpdate(method, 1, (key, count) => count + 1);
        }

        public void RecordImpressionsStats(RecordsEnum data, long count)
        {
            _records.AddOrUpdate(data, count, (key, value) => value + count);
        }

        public void RecordLatency(MethodEnum method, int bucket)
        {
            _latencies[method].Add(bucket);
        }

        public void RecordNonReadyUsages()
        {
            throw new NotImplementedException();
        }

        public void RecordSessionLength(long session)
        {
            _records.AddOrUpdate(RecordsEnum.Session, session, (key, value) => session);
        }

        public void RecordStreamingEvent(StreamingEvent streamingEvent)
        {
            lock (_streamingEventsLock)
            {
                _streamingEvents.Add(streamingEvent);
            }
        }

        public void RecordSucccessfulSync(RecordsEnum resource, long timestamp)
        {
            _records.AddOrUpdate(resource, timestamp, (key, value) => timestamp);
        }

        public void RecordSyncError(ResourceEnum resource, int status)
        {
            _httpErrors[resource].AddOrUpdate(status, 1, (key, count) => count + 1);
        }

        public void RecordSyncLatency(string path, int bucket)
        {
            throw new NotImplementedException();
        }

        public void RecordTokenRefreshes()
        {
            throw new NotImplementedException();
        }

        #region Private Methods
        private void InitLatencies()
        {
            _latencies.TryAdd(MethodEnum.Treatment, new List<long>());
            _latencies.TryAdd(MethodEnum.Treatments, new List<long>());
            _latencies.TryAdd(MethodEnum.TreatmentWithConfig, new List<long>());
            _latencies.TryAdd(MethodEnum.TreatmentsWithConfig, new List<long>());
            _latencies.TryAdd(MethodEnum.Track, new List<long>());
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
