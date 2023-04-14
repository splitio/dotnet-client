using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Impressions.Classes
{
    public class RedisImpressionLog : IImpressionsLog
    {
        private readonly IRedisImpressionsCache _impressionsCache;

        public RedisImpressionLog(IRedisImpressionsCache impressionsCache)
        {
            _impressionsCache = impressionsCache;
        }

        public async Task<int> LogAsync(IList<KeyImpression> impressions)
        {
            return await _impressionsCache.AddItemsAsync(impressions);
        }

        public void Start()
        {
            throw new System.NotImplementedException();
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }
    }
}
