using Splitio.Telemetry.Domain;

namespace Splitio.Telemetry.Common
{
    public interface ITelemetryAPI
    {
        void RecordConfigInit(Config init);
        void RecordStats(Stats stats);
    }
}
