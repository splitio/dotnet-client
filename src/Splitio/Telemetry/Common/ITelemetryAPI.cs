using Splitio.Telemetry.Domain;
using System.Threading.Tasks;

namespace Splitio.Telemetry.Common
{
    public interface ITelemetryAPI
    {
        Task RecordConfigInitAsync(Config init);
        Task RecordStatsAsync(Stats stats);
        Task RecordUniqueKeysAsync(UniqueKeys uniqueKeys);
    }
}
