using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Telemetry.Domain;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisImpressionsCache : RedisCacheBase, IImpressionsCache
    {
        private static readonly TimeSpan _expireTimeOneHour = new TimeSpan(0, 0, 3600);

        private readonly IRedisAdapterProducer _redisAdapterProducer;

        private string UniqueKeysKey => $"{RedisKeyPrefix}uniquekeys";
        private string ImpressionsCountKey => $"{RedisKeyPrefix}impressions.count";
        private string ImpressionsKey => $"{RedisKeyPrefix}impressions";

        public RedisImpressionsCache(IRedisAdapterProducer redisAdapter, RedisConfig redisConfig, bool clusterMode) : base(redisConfig, clusterMode)
        {
            _redisAdapterProducer = redisAdapter;
        }

        public int Add(IList<KeyImpression> items)
        {
            var lengthRedis = _redisAdapterProducer.ListRightPush(ImpressionsKey, GetImpressions(items));

            if (lengthRedis == items.Count)
            {
                _redisAdapterProducer.KeyExpire(ImpressionsKey, _expireTimeOneHour);
            }

            return 0;
        }

        public async Task<int> AddAsync(IList<KeyImpression> items)
        {
            var lengthRedis = await _redisAdapterProducer.ListRightPushAsync(ImpressionsKey, GetImpressions(items));

            if (lengthRedis == items.Count)
            {
                await _redisAdapterProducer.KeyExpireAsync(ImpressionsKey, _expireTimeOneHour);
            }

            return 0;
        }

        public async Task RecordUniqueKeysAsync(List<Mtks> uniqueKeys)
        {
            var lengthRedis = 0L;
            foreach (var item in uniqueKeys)
            {
                lengthRedis = await _redisAdapterProducer.ListRightPushAsync(UniqueKeysKey, JsonConvert.SerializeObject(item));
            }

            // This operation will simply do nothing if the key no longer exists (queue is empty)
            // It's only done in the "successful" exit path so that the TTL is not overridden if mtks weren't
            // popped correctly. This will result in mtks getting lost but will prevent the queue from taking
            // a huge amount of memory.
            if (lengthRedis == uniqueKeys.Count)
            {
                await _redisAdapterProducer.KeyExpireAsync(UniqueKeysKey, _expireTimeOneHour);
            }
        }

        public async Task RecordImpressionsCountAsync(Dictionary<string, int> impressionsCount)
        {
            var result = await _redisAdapterProducer.HashIncrementBatchAsync(ImpressionsCountKey, impressionsCount);

            if (result == (impressionsCount.Count + impressionsCount.Sum(i => i.Value)))
            {
                await _redisAdapterProducer.KeyExpireAsync(UniqueKeysKey, _expireTimeOneHour);
            }
        }

        private RedisValue[] GetImpressions(IList<KeyImpression> items)
        {
            var impressions = items.Select(item => JsonConvert.SerializeObject(new
            {
                m = new { s = SdkVersion, i = MachineIp, n = MachineName },
                i = new { k = item.KeyName, b = item.BucketingKey, f = item.Feature, t = item.Treatment, r = item.Label, c = item.ChangeNumber, m = item.Time, pt = item.PreviousTime }
            }));

            return impressions
                .Select(i => (RedisValue)i)
                .ToArray();
        }
    }
}
