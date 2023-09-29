using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Impressions.Classes
{
    public class RedisImpressionLog : IImpressionsLog
    {
        private readonly IImpressionsCache _impressionsCache;

        public RedisImpressionLog(IImpressionsCache impressionsCache)
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
