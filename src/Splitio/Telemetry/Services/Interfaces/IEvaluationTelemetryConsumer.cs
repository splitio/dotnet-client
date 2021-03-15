using Splitio.Telemetry.Domain;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IEvaluationTelemetryConsumer
    {
        MethodLatencies PopLatencies();
        MethodExceptions PopExceptions();
    }
}
