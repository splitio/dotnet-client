using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IImpressionTelemetryConsumer
    {
        long GetImpressionsStats(ImpressionsEnum data);
    }
}
