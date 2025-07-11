using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisEventsCache : RedisCacheBase, ISimpleCache<WrappedEvent>
    {
        private readonly IRedisAdapterProducer _redisAdapterProducer;

        public RedisEventsCache(IRedisAdapterProducer redisAdapter, RedisConfig redisConfig, bool clusterMode) : base(redisConfig, clusterMode)
        {
            _redisAdapterProducer = redisAdapter;
        }

        public int AddItems(IList<WrappedEvent> items)
        {
            if (!items.Any()) return 0;

            var value = JsonConvertWrapper.SerializeObject(items.FirstOrDefault());
            return (int)_redisAdapterProducer.ListRightPush($"{RedisKeyPrefix}events", value);
        }

        public async Task<int> AddItemsAsync(IList<WrappedEvent> items)
        {
            if (!items.Any()) return 0;

            var value = JsonConvertWrapper.SerializeObject(items.FirstOrDefault());
            return (int)await _redisAdapterProducer.ListRightPushAsync($"{RedisKeyPrefix}events", value);
        }
    }
}
