using Splitio.Telemetry.Domain;

namespace Splitio.Telemetry.Storages
{
    public interface ITelemetryStorageProducer
    {
        void RecordLatency(MethodEnum method, int bucket);
        void RecordException(MethodEnum method);
        void RecordSucccessfulSync(RecordsEnum method, long timestamp);
        void RecordSyncError(ResourceEnum resuource, int status);
        void RecordSyncLatency(string path, int bucket);
        void RecordAuthRejections();
        void RecordTokenRefreshes();
        void RecordImpressionsStats(RecordsEnum data, long count);
        void RecordStreamingEvent(StreamingEvent streamingEvent);
        void RecordSessionLength(long session);
        void RecordNonReadyUsages();
        void RecordBURTimeout();
        void RecordEventsStats(RecordsEnum data, long count);
        void AddTag(string tag);
    }
}
