using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IImpressionTelemetryProducer
    {
        void RecordImpressionsStats(ImpressionsEnum data, long count);
    }
}
