namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IPushTelemetryConsumer
    {
        long PopAuthRejections();
        long PopTokenRefreshes();
    }
}
