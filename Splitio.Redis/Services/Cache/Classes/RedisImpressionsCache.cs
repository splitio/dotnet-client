using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Telemetry.Domain;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisImpressionsCache : RedisCacheBase, IRedisImpressionsCache
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

        public async Task<int> AddItemsAsync(IList<KeyImpression> items)
        {
            var impressions = items.Select(item => JsonConvert.SerializeObject(new
            {
                m = new { s = SdkVersion, i = MachineIp, n = MachineName },
                i = new { k = item.keyName, b = item.bucketingKey, f = item.feature, t = item.treatment, r = item.label, c = item.changeNumber, m = item.time, pt = item.previousTime }
            }));

            var lengthRedis = await _redisAdapter.ListRightPushAsync(ImpressionsKey, impressions.Select(i => (RedisValue)i).ToArray());

            if (lengthRedis == items.Count)
            {
                await _redisAdapter.KeyExpireAsync(ImpressionsKey, _expireTimeOneHour);
            }

            return 0;
        }

        public async Task RecordUniqueKeysAsync(List<Mtks> uniqueKeys)
        {
            var lengthRedis = 0L;
            foreach (var item in uniqueKeys)
            {
                var value = new RedisValue[] { JsonConvert.SerializeObject(item) };
                lengthRedis = await _redisAdapter.ListRightPushAsync(UniqueKeysKey, value);
            }

            // This operation will simply do nothing if the key no longer exists (queue is empty)
            // It's only done in the "successful" exit path so that the TTL is not overridden if mtks weren't
            // popped correctly. This will result in mtks getting lost but will prevent the queue from taking
            // a huge amount of memory.
            if (lengthRedis == uniqueKeys.Count)
            {
                await _redisAdapter.KeyExpireAsync(UniqueKeysKey, _expireTimeOneHour);
            }
        }

        public async Task RecordImpressionsCountAsync(Dictionary<string, int> impressionsCount)
        {
            var result = _redisAdapter.HashIncrementBatch(ImpressionsCountKey, impressionsCount);

            if (result == (impressionsCount.Count + impressionsCount.Sum(i => i.Value)))
            {
                await _redisAdapter.KeyExpireAsync(UniqueKeysKey, _expireTimeOneHour);
            }
        }

        public List<KeyImpression> FetchAllAndClear()
        {
            throw new NotImplementedException(); // No-op
        }

        public bool HasReachedMaxSize()
        {
            throw new NotImplementedException(); // No-op
        }

        public bool IsEmpty()
        {
            throw new NotImplementedException(); // No-op
        }
    }
}
