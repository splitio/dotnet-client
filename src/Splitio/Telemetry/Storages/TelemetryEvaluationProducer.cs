using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Storages
{
    public class TelemetryEvaluationProducer : TelemetryStorageBase, ITelemetryEvaluationProducer
    {
        public TelemetryEvaluationProducer(InMemoryTelemetryStorage telemetryStorage) : base(telemetryStorage)
        {
        }

        public void RecordLatency(MethodEnum method, int bucket)
        {
            _telemetryStorage.MethodLatencies[method].Add(bucket);
        }

        public void RecordException(MethodEnum method)
        {
            _telemetryStorage.ExceptionsCounters.AddOrUpdate(method, 1, (key, count) => count + 1);
        }
    }
}
