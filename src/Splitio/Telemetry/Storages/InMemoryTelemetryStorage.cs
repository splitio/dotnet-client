using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Telemetry.Storages
{
    public class InMemoryTelemetryStorage
    {
        // Latencies
        public readonly ConcurrentDictionary<MethodEnum, IList<long>> MethodLatencies = new ConcurrentDictionary<MethodEnum, IList<long>>();
        public readonly ConcurrentDictionary<ResourceEnum, IList<long>> HttpLatencies = new ConcurrentDictionary<ResourceEnum, IList<long>>();

        // Counters
        public readonly ConcurrentDictionary<MethodEnum, long> ExceptionsCounters = new ConcurrentDictionary<MethodEnum, long>();
        public readonly ConcurrentDictionary<PushCountersEnum, long> PushCounters = new ConcurrentDictionary<PushCountersEnum, long>();
        public readonly ConcurrentDictionary<FactoryCountersEnum, long> FactoryCounters = new ConcurrentDictionary<FactoryCountersEnum, long>();

        // Records
        public readonly ConcurrentDictionary<ImpressionsEnum, long> ImpressionsDataRecords = new ConcurrentDictionary<ImpressionsEnum, long>();
        public readonly ConcurrentDictionary<EventsEnum, long> EventsDataRecords = new ConcurrentDictionary<EventsEnum, long>();
        public readonly ConcurrentDictionary<ResourceEnum, long> LastSynchronizationRecords = new ConcurrentDictionary<ResourceEnum, long>();
        public readonly ConcurrentDictionary<SdkRecordsEnum, long> SdkRecords = new ConcurrentDictionary<SdkRecordsEnum, long>();
        public readonly ConcurrentDictionary<FactoryRecordsEnum, long> FactoryRecords = new ConcurrentDictionary<FactoryRecordsEnum, long>();

        // HttpErrors
        public readonly ConcurrentDictionary<ResourceEnum, ConcurrentDictionary<int, long>> HttpErrors = new ConcurrentDictionary<ResourceEnum, ConcurrentDictionary<int, long>>();

        // Tags
        public readonly IList<string> Tags = new List<string>();
        public readonly object TagsLock = new object();

        // Streaming Events
        public readonly IList<StreamingEvent> StreamingEvents = new List<StreamingEvent>();
        public readonly object StreamingEventsLock = new object();

        public InMemoryTelemetryStorage()
        {
            InitLatencies();
            InitHttpLatencies();
            InitHttpErrors();
        }

        public void InitLatencies()
        {
            MethodLatencies.TryAdd(MethodEnum.Treatment, new List<long>());
            MethodLatencies.TryAdd(MethodEnum.Treatments, new List<long>());
            MethodLatencies.TryAdd(MethodEnum.TreatmentWithConfig, new List<long>());
            MethodLatencies.TryAdd(MethodEnum.TreatmentsWithConfig, new List<long>());
            MethodLatencies.TryAdd(MethodEnum.Track, new List<long>());
        }

        public void InitHttpLatencies()
        {
            HttpLatencies.TryAdd(ResourceEnum.SplitSync, new List<long>());
            HttpLatencies.TryAdd(ResourceEnum.SegmentSync, new List<long>());
            HttpLatencies.TryAdd(ResourceEnum.ImpressionSync, new List<long>());
            HttpLatencies.TryAdd(ResourceEnum.ImpressionCountSync, new List<long>());
            HttpLatencies.TryAdd(ResourceEnum.EventSync, new List<long>());
            HttpLatencies.TryAdd(ResourceEnum.TelemetrySync, new List<long>());
            HttpLatencies.TryAdd(ResourceEnum.TokenSync, new List<long>());
        }

        public void InitHttpErrors()
        {
            HttpErrors.TryAdd(ResourceEnum.EventSync, new ConcurrentDictionary<int, long>());
            HttpErrors.TryAdd(ResourceEnum.ImpressionSync, new ConcurrentDictionary<int, long>());
            HttpErrors.TryAdd(ResourceEnum.ImpressionCountSync, new ConcurrentDictionary<int, long>());
            HttpErrors.TryAdd(ResourceEnum.SegmentSync, new ConcurrentDictionary<int, long>());
            HttpErrors.TryAdd(ResourceEnum.SplitSync, new ConcurrentDictionary<int, long>());
            HttpErrors.TryAdd(ResourceEnum.TelemetrySync, new ConcurrentDictionary<int, long>());
            HttpErrors.TryAdd(ResourceEnum.TokenSync, new ConcurrentDictionary<int, long>());
        }
    }
}
