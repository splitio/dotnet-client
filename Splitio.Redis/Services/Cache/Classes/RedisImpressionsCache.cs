using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Telemetry.Domain;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisImpressionsCache : RedisCacheBase, IImpressionsCache
    {
        private static readonly TimeSpan _expireTimeOneHour = new TimeSpan(0, 0, 3600);

        private string UniqueKeysKey => "{prefix}.SPLITIO.uniquekeys"
            .Replace("{prefix}.", string.IsNullOrEmpty(UserPrefix) ? string.Empty : $"{UserPrefix}.");

        private string ImpressionsCountKey => "{prefix}.SPLITIO.impressions.count"
            .Replace("{prefix}.", string.IsNullOrEmpty(UserPrefix) ? string.Empty : $"{UserPrefix}.");

        private string ImpressionsKey => "{prefix}.SPLITIO.impressions"
            .Replace("{prefix}.", string.IsNullOrEmpty(UserPrefix) ? string.Empty : $"{UserPrefix}.");
        
        public RedisImpressionsCache(IRedisAdapter redisAdapter, 
            string machineIP, 
            string sdkVersion, 
            string machineName, 
            string userPrefix = null) : base(redisAdapter, machineIP, sdkVersion, machineName, userPrefix) 
        { }

        public int AddItems(IList<KeyImpression> items)
        {
            var impressions = items.Select(item => JsonConvert.SerializeObject(new
            {
                m = new { s = SdkVersion, i = MachineIp, n = MachineName },
                i = new { k = item.keyName, b = item.bucketingKey, f = item.feature, t = item.treatment, r = item.label, c = item.changeNumber, m = item.time, pt = item.previousTime }
            }));

            var lengthRedis = _redisAdapter.ListRightPush(ImpressionsKey, impressions.Select(i => (RedisValue)i).ToArray());

            if (lengthRedis == items.Count)
            {
                _redisAdapter.KeyExpire(ImpressionsKey, _expireTimeOneHour);
            }

            return 0;
        }

        public void RecordUniqueKeys(List<Mtks> uniqueKeys)
        {
            var lengthRedis = _redisAdapter.ListRightPush(UniqueKeysKey, JsonConvert.SerializeObject(uniqueKeys));

            // This operation will simply do nothing if the key no longer exists (queue is empty)
            // It's only done in the "successful" exit path so that the TTL is not overridden if mtks weren't
            // popped correctly. This will result in mtks getting lost but will prevent the queue from taking
            // a huge amount of memory.
            if (lengthRedis == uniqueKeys.Count)
            {
                _redisAdapter.KeyExpire(UniqueKeysKey, _expireTimeOneHour);
            }
        }

        public void RecordImpressionsCount(Dictionary<string, int> impressionsCount)
        {
            var result = _redisAdapter.HashIncrementAsyncBatch(ImpressionsCountKey, impressionsCount);

            if (result == (impressionsCount.Count + impressionsCount.Sum(i => i.Value)))
            {
                _redisAdapter.KeyExpire(UniqueKeysKey, _expireTimeOneHour);
            }
        }
    }
}
