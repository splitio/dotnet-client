using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using System.Collections.Generic;

namespace Splitio.Telemetry.Storages
{
    public interface ITelemetryStorageConsumer : ITelemetryRuntimeConsumer, ITelemetryInitConsumer, ITelemetryEvaluationConsumer
    {        
    }

    public interface ITelemetryRuntimeConsumer
    {
        long GetImpressionsStats(ImpressionsEnum data);
        long GetEventsStats(EventsEnum data);
        LastSynchronization GetLastSynchronizations();
        HTTPErrors PopHttpErrors();
        HTTPLatencies PopHttpLatencies();
        long PopAuthRejections();
        long PopTokenRefreshes();
        IList<StreamingEvent> PopStreamingEvents();
        IList<string> PopTags();
        long GetSessionLength();
        UpdatesFromSSE PopUpdatesFromSSE();
    }

    public interface ITelemetryInitConsumer
    {
        long GetNonReadyUsages();
        long GetBURTimeouts();
    }

    public interface ITelemetryEvaluationConsumer
    {
        MethodExceptions PopExceptions();
        MethodLatencies PopLatencies();
    }
}
