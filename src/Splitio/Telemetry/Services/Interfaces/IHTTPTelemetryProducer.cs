using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IHTTPTelemetryProducer
    {
        void RecordSyncError(ResourceEnum resuource, int status);
        void RecordSyncLatency(ResourceEnum resource, long latency);
    }
}
