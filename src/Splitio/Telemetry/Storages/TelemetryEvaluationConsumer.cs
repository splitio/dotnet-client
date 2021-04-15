using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Storages
{
    public class TelemetryEvaluationConsumer : TelemetryStorageBase, ITelemetryEvaluationConsumer
    {
        public TelemetryEvaluationConsumer(InMemoryTelemetryStorage telemetryStorage) : base(telemetryStorage)
        {
        }

        public MethodExceptions PopExceptions()
        {
            var exceptions = new MethodExceptions
            {
                Treatment = _telemetryStorage.ExceptionsCounters.TryGetValue(MethodEnum.Treatment, out long treatmentValue) ? treatmentValue : 0,
                Treatments = _telemetryStorage.ExceptionsCounters.TryGetValue(MethodEnum.Treatments, out long treatmentsValue) ? treatmentsValue : 0,
                TreatmentWithConfig = _telemetryStorage.ExceptionsCounters.TryGetValue(MethodEnum.TreatmentWithConfig, out long twcValue) ? twcValue : 0,
                TreatmentsWithConfig = _telemetryStorage.ExceptionsCounters.TryGetValue(MethodEnum.TreatmentsWithConfig, out long tswcValue) ? tswcValue : 0,
                Track = _telemetryStorage.ExceptionsCounters.TryGetValue(MethodEnum.Track, out long trackValue) ? trackValue : 0,
            };

            _telemetryStorage.ExceptionsCounters.Clear();

            return exceptions;
        }

        public MethodLatencies PopLatencies()
        {
            var latencies = new MethodLatencies
            {
                Treatment = _telemetryStorage.MethodLatencies[MethodEnum.Treatment],
                Treatments = _telemetryStorage.MethodLatencies[MethodEnum.Treatments],
                TreatmenstWithConfig = _telemetryStorage.MethodLatencies[MethodEnum.TreatmentsWithConfig],
                TreatmentWithConfig = _telemetryStorage.MethodLatencies[MethodEnum.TreatmentWithConfig],
                Track = _telemetryStorage.MethodLatencies[MethodEnum.Track]
            };

            _telemetryStorage.MethodLatencies.Clear();
            _telemetryStorage.InitLatencies();

            return latencies;
        }        
    }
}
