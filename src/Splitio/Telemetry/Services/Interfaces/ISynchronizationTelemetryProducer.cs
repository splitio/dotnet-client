using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface ISynchronizationTelemetryProducer
    {
        void RecordSuccessfulSync(ResourceEnum resource);
    }
}
