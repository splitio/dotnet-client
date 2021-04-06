﻿using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Storages
{
    public interface ITelemetryStorageProducer : ITelemetryEvaluationProducer, ITelemetryRuntimeProducer, ITelemetryInitProducer
    {
    }

    public interface ITelemetryEvaluationProducer
    {
        void RecordLatency(MethodEnum method, int bucket);
        void RecordException(MethodEnum method);
    }

    public interface ITelemetryRuntimeProducer
    {
        void AddTag(string tag);
        void RecordImpressionsStats(ImpressionsEnum data, long count);
        void RecordEventsStats(EventsEnum data, long count);
        void RecordSuccessfulSync(ResourceEnum resource, long timestamp);
        void RecordSyncError(ResourceEnum resuource, int status);
        void RecordSyncLatency(ResourceEnum resource, int bucket);
        void RecordAuthRejections();
        void RecordTokenRefreshes();
        void RecordStreamingEvent(StreamingEvent streamingEvent);
        void RecordSessionLength(long session);
    }

    public interface ITelemetryInitProducer
    {
        void RecordConfigInit(Config config);
        void RecordNonReadyUsages();
        void RecordBURTimeout();
    }
}
