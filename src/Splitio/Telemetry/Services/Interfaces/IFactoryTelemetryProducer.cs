namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IFactoryTelemetryProducer
    {
        void RecordNonReadyUsages();
        void RecordBURTimeout();
    }
}
