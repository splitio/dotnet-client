using Splitio.Domain;
using Splitio.Redis.Services.Impressions.Interfaces;
using Splitio.Services.Shared.Interfaces;

namespace Splitio.Redis.Services.Cache.Interfaces
{
    public interface IImpressionsCache : ISimpleCache<KeyImpression>, IRedisUniqueKeysStorage, IRedisImpressionCountStorage
    {
    }
}
