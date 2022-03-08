using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisImpressionsCache : RedisCacheBase, IImpressionsCache
    {
        public RedisImpressionsCache(IRedisAdapter redisAdapter, 
            string machineIP, 
            string sdkVersion, 
            string machineName, 
            string userPrefix = null) : base(redisAdapter, machineIP, sdkVersion, machineName, userPrefix) 
        { }

        public int AddItems(IList<KeyImpression> items)
        {
            var key = string.Format("{0}SPLITIO.impressions", string.IsNullOrEmpty(UserPrefix) ? string.Empty : $"{UserPrefix}.");

            var impressions = items.Select(item => JsonConvert.SerializeObject(new
            {
                m = new { s = SdkVersion, i = MachineIp, n = MachineName },
                i = new { k = item.keyName, b = item.bucketingKey, f = item.feature, t = item.treatment, r = item.label, c = item.changeNumber, m = item.time }
            }));

            var lengthRedis = _redisAdapter.ListRightPush(key, impressions.Select(i => (RedisValue)i).ToArray());

            if (lengthRedis == items.Count)
            {
                _redisAdapter.KeyExpire(key, new TimeSpan(0, 0, 3600));
            }

            return 0;
        }

        public void RecordUniqueKeys(List<string> uniqueKeys)
        {
            var key = "{prefix}.SPLITIO.uniquekeys".Replace("{prefix}.", string.IsNullOrEmpty(UserPrefix) ? string.Empty : $"{UserPrefix}.");
            var uniques = uniqueKeys.Select(x => (RedisValue)x).ToArray();

            _redisAdapter.SAdd(key, uniques);
        }

        public void RecordImpressionsCount(Dictionary<string, int> impressionsCount)
        {
            var key = "{prefix}.SPLITIO.impressions.count".Replace("{prefix}.", string.IsNullOrEmpty(UserPrefix) ? string.Empty : $"{UserPrefix}.");

            _redisAdapter.HashIncrementAsyncBatch(key, impressionsCount);
        }
    }
}
