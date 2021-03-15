using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using System.Collections.Generic;

namespace Splitio.Telemetry.Storages
{
    public interface ITelemetryStorageConsumer
    {
        MethodLatencies PopLatencies();
        MethodExceptions PopExceptions();
        LastSynchronization GetLastSynchronizations();
        HTTPErrors PopHttpErrors();
        HTTPLatencies PopHttpLatencies();
        long PopAuthRejections();
        long PopTokenRefreshes();
        IList<StreamingEvent> PopStreamingEvents();
        IList<string> PopTags();
        long GetSessionLength();
        long GetNonReadyUsages();
        long GetBURTimeouts();
        long GetEventsStats(EventsEnum data);
        long GetImpressionsStats(ImpressionsEnum data);
    }
}
