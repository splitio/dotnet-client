using Splitio.Telemetry.Domain;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IHTTPTelemetryConsumer
    {
        HTTPErrors PopHttpErrors();
        HTTPLatencies PopHttpLatencies();
    }
}
