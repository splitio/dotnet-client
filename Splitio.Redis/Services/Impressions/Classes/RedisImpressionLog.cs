using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using System.Collections.Generic;

namespace Splitio.Redis.Services.Impressions.Classes
{
    public class RedisImpressionLog : IImpressionsLog
    {
        private readonly IRedisImpressionsCache _impressionsCache;

        public RedisImpressionLog(IRedisImpressionsCache impressionsCache)
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

        public void Stop()
        {
            throw new System.NotImplementedException();
        }
    }
}
