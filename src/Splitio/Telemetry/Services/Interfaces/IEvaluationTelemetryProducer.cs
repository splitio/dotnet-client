using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Telemetry.Services.Interfaces
{
    public interface IEvaluationTelemetryProducer
    {
        void RecordLatency(MethodEnum method, long latency);
        void RecordException(MethodEnum method);
    }
}
