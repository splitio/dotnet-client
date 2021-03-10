namespace Splitio.Telemetry.Services.Interfaces
{
    public interface ISDKInfoTelemetryProducer
    {
        void RecordSessionLength(long session);
    }
}
