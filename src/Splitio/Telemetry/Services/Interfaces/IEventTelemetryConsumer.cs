using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IEventTelemetryConsumer
    {
        long GetEventsStats(EventsEnum data);
    }
}
