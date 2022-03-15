using Splitio.Telemetry.Domain;
using System.Collections.Generic;

namespace Splitio.Redis.Services.Impressions.Interfaces
{
    public interface IRedisUniqueKeysStorage
    {
        void RecordUniqueKeys(List<Mtks> uniqueKeys);
    }

    public interface IRedisImpressionCountStorage
    {
        void RecordImpressionsCount(Dictionary<string, int> impressionsCount);
    }
}
