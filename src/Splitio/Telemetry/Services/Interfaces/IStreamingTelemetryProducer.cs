using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IStreamingTelemetryProducer
    {
        void RecordStreamingEvent(EventTypeEnum type, long data);
    }
}
