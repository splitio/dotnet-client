namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IFactoryTelemetryConsumer
    {
        long GetNonReadyUsages();
        long GetBURTimeouts();
    }
}
