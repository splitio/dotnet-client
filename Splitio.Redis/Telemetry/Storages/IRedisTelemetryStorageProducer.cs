using Splitio.Telemetry.Storages;

namespace Splitio.Redis.Telemetry.Storages
{
    public interface IRedisTelemetryStorageProducer : ITelemetryInitProducer, ITelemetryEvaluationProducer
    {
    }
}
