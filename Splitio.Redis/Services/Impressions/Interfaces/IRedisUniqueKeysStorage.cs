using Splitio.Telemetry.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Impressions.Interfaces
{
    public interface IRedisUniqueKeysStorage
    {
        Task RecordUniqueKeysAsync(List<Mtks> uniqueKeys);
    }

    public interface IRedisImpressionCountStorage
    {
        Task RecordImpressionsCountAsync(Dictionary<string, int> impressionsCount);
    }
}
