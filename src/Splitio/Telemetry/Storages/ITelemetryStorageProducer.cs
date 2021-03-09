using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Storages
{
    public interface ITelemetryStorageProducer
    {
        void RecordLatency(MethodEnum method, int bucket);
        void RecordException(MethodEnum method);
        void RecordSuccessfulSync(LastSynchronizationRecordsEnum method, long timestamp);
        void RecordSyncError(ResourceEnum resuource, int status);
        void RecordSyncLatency(HttpLatenciesEnum path, int bucket);
        void RecordAuthRejections();
        void RecordTokenRefreshes();
        void RecordImpressionsStats(ImpressionsDataRecordsEnum data, long count);
        void RecordStreamingEvent(StreamingEvent streamingEvent);
        void RecordSessionLength(long session);
        void RecordNonReadyUsages();
        void RecordBURTimeout();
        void RecordEventsStats(EventsDataRecordsEnum data, long count);
        void AddTag(string tag);
    }
}
