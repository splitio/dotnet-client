using System.Collections.Generic;
using System.Threading.Tasks;
using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Shared.Interfaces;

namespace Splitio.Redis.Services.Impressions.Classes
{
    public class RedisImpressionLog : IImpressionsLog
    {
        private readonly ISimpleCache<KeyImpression> _impressionsCache;

        public RedisImpressionLog(ISimpleCache<KeyImpression> impressionsCache)
        {
            _impressionsCache = impressionsCache;
        }

        public int Log(IList<KeyImpression> impressions)
        {
            return _impressionsCache.AddItems(impressions);
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
