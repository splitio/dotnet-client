using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Storages
{
    public class TelemetryRuntimeProducer : TelemetryStorageBase, ITelemetryRuntimeProducer
    {
        public TelemetryRuntimeProducer(InMemoryTelemetryStorage telemetryStorage) : base(telemetryStorage)
        {
        }

        public void AddTag(string tag)
        {
            lock (_telemetryStorage.TagsLock)
            {
                _telemetryStorage.Tags.Add(tag);
            }
        }

        public void RecordAuthRejections()
        {
            _telemetryStorage.PushCounters.AddOrUpdate(PushCountersEnum.AuthRejecttions, 1, (key, value) => value + 1);
        }

        public void RecordEventsStats(EventsEnum data, long count)
        {
            _telemetryStorage.EventsDataRecords.AddOrUpdate(data, count, (key, value) => value + count);
        }

        public void RecordImpressionsStats(ImpressionsEnum data, long count)
        {
            if (count <= 0) return;

            _telemetryStorage.ImpressionsDataRecords.AddOrUpdate(data, count, (key, value) => value + count);
        }

        public void RecordSessionLength(long session)
        {
            _telemetryStorage.SdkRecords.AddOrUpdate(SdkRecordsEnum.Session, session, (key, value) => session);
        }

        public void RecordStreamingEvent(StreamingEvent streamingEvent)
        {
            lock (_telemetryStorage.StreamingEventsLock)
            {
                _telemetryStorage.StreamingEvents.Add(streamingEvent);
            }
        }

        public void RecordSuccessfulSync(ResourceEnum resource, long timestamp)
        {
            _telemetryStorage.LastSynchronizationRecords.AddOrUpdate(resource, timestamp, (key, value) => timestamp);
        }

        public void RecordSyncError(ResourceEnum resource, int status)
        {
            _telemetryStorage.HttpErrors[resource].AddOrUpdate(status, 1, (key, count) => count + 1);
        }

        public void RecordSyncLatency(ResourceEnum resource, int bucket)
        {
            _telemetryStorage.HttpLatencies[resource].Add(bucket);
        }

        public void RecordTokenRefreshes()
        {
            _telemetryStorage.PushCounters.AddOrUpdate(PushCountersEnum.TokenRefreshes, 1, (key, value) => value + 1);
        }
    }
}
