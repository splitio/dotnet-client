using Splitio.Domain;
using Splitio.Redis.Services.Impressions.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Interfaces
{
    public interface IImpressionsCache : IRedisUniqueKeysStorage, IRedisImpressionCountStorage
    {
        int Add(IList<KeyImpression> impressions);
        Task<int> AddAsync(IList<KeyImpression> impressions);
    }
}
