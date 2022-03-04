using System.Collections.Generic;

namespace Splitio.Redis.Services.Impressions.Interfaces
{
    public interface IRedisUniqueKeysStorage
    {
        void RecordUniqueKeys(List<string> uniqueKeys);
    }
}
