using Splitio.Commons.Domain;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Impressions.Classes
{
    public class RedisImpressionLog : Commons.Cache.Interfaces.IImpressionsCache
    {
        private readonly Cache.Interfaces.IImpressionsCache _impressionsCache;

        public RedisImpressionLog(Cache.Interfaces.IImpressionsCache impressionsCache)
        {
            _impressionsCache = impressionsCache;
        }

        public int Log(IList<KeyImpression> impressions)
        {
            return _impressionsCache.Add(impressions);
        }

        public async Task<int> LogAsync(IList<KeyImpression> impressions)
        {
            return await _impressionsCache.AddAsync(impressions);
        }

        public void Start()
        {
            throw new System.NotImplementedException();
        }

        public Task StopAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}
