using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;

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
            foreach (var item in items)
            {
                var eventJson = JsonConvert.SerializeObject(new
                {
                    m = new { s = _sdkVersion, i = _machineIP, n = _machineName },
                    e = item.Event
                });

                _redisAdapterProducer.ListRightPush($"{RedisKeyPrefix}events", eventJson);
            }

            return 0;
        }
    }
}
