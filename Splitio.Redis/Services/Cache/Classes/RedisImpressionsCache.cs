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
        private readonly object _lock = new object();

        private readonly IRedisAdapterProducer _redisAdapterProducer;

        private string UniqueKeysKey => "{prefix}.SPLITIO.uniquekeys"
            .Replace("{prefix}.", string.IsNullOrEmpty(UserPrefix) ? string.Empty : $"{UserPrefix}.");

        private string ImpressionsCountKey => "{prefix}.SPLITIO.impressions.count"
            .Replace("{prefix}.", string.IsNullOrEmpty(UserPrefix) ? string.Empty : $"{UserPrefix}.");

        private string ImpressionsKey => "{prefix}.SPLITIO.impressions"
            .Replace("{prefix}.", string.IsNullOrEmpty(UserPrefix) ? string.Empty : $"{UserPrefix}.");
        
        public RedisImpressionsCache(IRedisAdapterProducer redisAdapter, 
            string machineIP, 
            string sdkVersion, 
            string machineName, 
            string userPrefix = null) : base(machineIP, sdkVersion, machineName, userPrefix) 
        {
            _redisAdapterProducer = redisAdapter;
        }

        public int AddItems(IList<KeyImpression> items)
        {
            var impressions = items.Select(item => JsonConvert.SerializeObject(new
            {
                m = new { s = SdkVersion, i = MachineIp, n = MachineName },
                i = new { k = item.keyName, b = item.bucketingKey, f = item.feature, t = item.treatment, r = item.label, c = item.changeNumber, m = item.time, pt = item.previousTime }
            }));

            var lengthRedis = _redisAdapterProducer.ListRightPush(ImpressionsKey, impressions.Select(i => (RedisValue)i).ToArray());

            if (lengthRedis == items.Count)
            {
                _redisAdapterProducer.KeyExpire(ImpressionsKey, _expireTimeOneHour);
            }

            return 0;
        }

        public void RecordUniqueKeys(List<Mtks> uniqueKeys)
        {
            lock (_lock)
            {
                var lengthRedis = 0L;
                foreach (var item in uniqueKeys)
                {
                    lengthRedis = _redisAdapterProducer.ListRightPush(UniqueKeysKey, JsonConvert.SerializeObject(item));
                }

                // This operation will simply do nothing if the key no longer exists (queue is empty)
                // It's only done in the "successful" exit path so that the TTL is not overridden if mtks weren't
                // popped correctly. This will result in mtks getting lost but will prevent the queue from taking
                // a huge amount of memory.
                if (lengthRedis == uniqueKeys.Count)
                {
                    _redisAdapterProducer.KeyExpire(UniqueKeysKey, _expireTimeOneHour);
                }
            }
        }

        public void RecordImpressionsCount(Dictionary<string, int> impressionsCount)
        {
            var result = _redisAdapterProducer.HashIncrementAsyncBatch(ImpressionsCountKey, impressionsCount);

            if (result == (impressionsCount.Count + impressionsCount.Sum(i => i.Value)))
            {
                _redisAdapterProducer.KeyExpire(UniqueKeysKey, _expireTimeOneHour);
            }
        }
    }
}
