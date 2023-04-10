using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Shared.Interfaces;
using StackExchange.Redis;
using System.Collections.Generic;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisEventsCache : RedisCacheBase, ISimpleCache<WrappedEvent>
    {
        private readonly string _machineName;
        private readonly string _machineIP;
        private readonly string _sdkVersion;

        public RedisEventsCache(IRedisAdapter redisAdapter,
            string machineName,
            string machineIP,
            string sdkVersion,
            string userPrefix = null) : base(redisAdapter, userPrefix)
        {
            _machineName = machineName;
            _machineIP = machineIP;
            _sdkVersion = sdkVersion;
        }

        public int AddItems(IList<WrappedEvent> items)
        {
            foreach (var item in items)
            {
                var value = new RedisValue[]
                {
                    JsonConvert.SerializeObject(new
                    {
                        m = new { s = _sdkVersion, i = _machineIP, n = _machineName },
                        e = item.Event
                    })
                };

                _redisAdapter.ListRightPushAsync($"{RedisKeyPrefix}events", value);
            }

            return 0;
        }
    }
}
