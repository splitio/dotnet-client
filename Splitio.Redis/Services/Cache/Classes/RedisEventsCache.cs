using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisEventsCache : RedisCacheBase, ISimpleCache<WrappedEvent>
    {
        private readonly string _machineName;
        private readonly string _machineIP;
        private readonly string _sdkVersion;

        private readonly IRedisAdapterProducer _redisAdapterProducer;

        public RedisEventsCache(IRedisAdapterProducer redisAdapter, 
            string machineName,
            string machineIP, 
            string sdkVersion, 
            string userPrefix = null) : base(userPrefix) 
        {
            _redisAdapterProducer = redisAdapter;
            _machineName = machineName;
            _machineIP = machineIP;
            _sdkVersion = sdkVersion;
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
                m = new { s = _sdkVersion, i = _machineIP, n = _machineName },
                e = wEvent.Event
            });
        }
    }
}
