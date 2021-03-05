using Splitio.Telemetry.Domain;
using System.Collections.Generic;

namespace Splitio.Telemetry.Storages
{
    public interface ITelemetryStorageConsumer
    {
        MethodLatencies PopLatencies();
        IDictionary<MethodEnum, long> PopExceptions();
        LastSynchronization GetLAstSynchronizations();
        HTTPErrors PopHttpErrors();
        HTTPLatencies PopHttpLatencies();
        long PopAuthRejections();
        long PopTokenRefreshes();        
        IList<StreamingEvent> PopStreamingEvents();
        IList<string> PopTags();
        long GetSessionLength();
        long GetNonReadyUsages();
        long GetBURTimeouts();
        long GetEventsStats(RecordsEnum data);
        long GetImpressionsStats(RecordsEnum data);
    }
}
