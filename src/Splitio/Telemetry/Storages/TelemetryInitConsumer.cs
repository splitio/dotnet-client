using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Storages
{
    public class TelemetryInitConsumer : TelemetryStorageBase, ITelemetryInitConsumer
    {
        public TelemetryInitConsumer(InMemoryTelemetryStorage telemetryStorage) : base(telemetryStorage)
        {
        }

        public long GetBURTimeouts()
        {
            if (!_telemetryStorage.FactoryCounters.TryGetValue(FactoryCountersEnum.BurTimeouts, out long value))
            {
                return 0;
            }

            return value;
        }

        public long GetNonReadyUsages()
        {
            if (!_telemetryStorage.FactoryCounters.TryGetValue(FactoryCountersEnum.NonReadyUsages, out long value))
            {
                return 0;
            }

            return value;
        }
    }
}
