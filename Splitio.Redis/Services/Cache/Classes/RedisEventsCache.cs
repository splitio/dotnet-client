using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
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

            return (int)_redisAdapterProducer.ListRightPush($"{RedisKeyPrefix}events", SerializeEventObject(items.FirstOrDefault()));
        }

        public async Task<int> AddItemsAsync(IList<WrappedEvent> items)
        {
            if (!items.Any()) return 0;

            return (int)await _redisAdapterProducer.ListRightPushAsync($"{RedisKeyPrefix}events", SerializeEventObject(items.FirstOrDefault()));
        }

        private string SerializeEventObject(WrappedEvent wEvent)
        {
            return JsonConvert.SerializeObject(new
            {
                m = new { s = SdkVersion, i = MachineIp, n = MachineName },
                e = wEvent.Event
            });
        }
    }
}
