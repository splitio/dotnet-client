using Splitio.Telemetry.Domain;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface ISynchronizationTelemetryConsumer
    {
        LastSynchronization GetLastSynchronizations();
    }
}
