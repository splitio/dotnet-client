namespace Splitio.Telemetry.Storages
{
    public class TelemetryStorageBase
    {
        protected readonly InMemoryTelemetryStorage _telemetryStorage;

        public TelemetryStorageBase(InMemoryTelemetryStorage telemetryStorage)
        {
            _telemetryStorage = telemetryStorage;
        }
    }
}
