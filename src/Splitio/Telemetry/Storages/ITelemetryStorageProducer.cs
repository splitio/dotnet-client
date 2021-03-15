using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Storages
{
    public interface ITelemetryStorageProducer
    {
        void RecordLatency(MethodEnum method, int bucket);
        void RecordException(MethodEnum method);
        void RecordSuccessfulSync(ResourceEnum resource, long timestamp);
        void RecordSyncError(ResourceEnum resuource, int status);
        void RecordSyncLatency(ResourceEnum resource, int bucket);
        void RecordAuthRejections();
        void RecordTokenRefreshes();
        void RecordImpressionsStats(ImpressionsEnum data, long count);
        void RecordStreamingEvent(StreamingEvent streamingEvent);
        void RecordSessionLength(long session);
        void RecordNonReadyUsages();
        void RecordBURTimeout();
        void RecordEventsStats(EventsEnum data, long count);
        void AddTag(string tag);
    }
}
