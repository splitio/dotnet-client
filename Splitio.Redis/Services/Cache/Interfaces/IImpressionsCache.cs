using Splitio.Redis.Services.Impressions.Interfaces;
using Splitio.Services.Impressions.Interfaces;

namespace Splitio.Redis.Services.Cache.Interfaces
{
    public interface IRedisImpressionsCache : IImpressionCache, IRedisUniqueKeysStorage, IRedisImpressionCountStorage
    {
    }
}
