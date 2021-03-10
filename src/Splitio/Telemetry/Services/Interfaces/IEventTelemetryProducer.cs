using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IEventTelemetryProducer
    {
        void RecordEventsStats(EventsEnum data, long count);
    }
}
