using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Storages
{
    public class TelemetryInitProducer : TelemetryStorageBase, ITelemetryInitProducer
    {
        public TelemetryInitProducer(InMemoryTelemetryStorage telemetryStorage) : base(telemetryStorage)
        {
        }

        public void RecordBURTimeout()
        {
            _telemetryStorage.FactoryCounters.AddOrUpdate(FactoryCountersEnum.BurTimeouts, 1, (key, value) => value + 1);
        }

        public void RecordConfigInit(Config config)
        {
            // No-Op. Config Data will be sent directly to Split Servers. No need to store.
        }

        public void RecordNonReadyUsages()
        {
            _telemetryStorage.FactoryCounters.AddOrUpdate(FactoryCountersEnum.NonReadyUsages, 1, (key, value) => value + 1);
        }
    }
}
