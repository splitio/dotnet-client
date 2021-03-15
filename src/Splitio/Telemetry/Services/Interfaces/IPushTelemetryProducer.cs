namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IPushTelemetryProducer
    {
        void RecordAuthRejections();
        void RecordTokenRefreshes();
    }
}
