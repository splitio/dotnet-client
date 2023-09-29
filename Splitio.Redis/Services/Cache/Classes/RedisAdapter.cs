using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;

namespace Splitio.Redis.Services.Cache.Classes
{
    public class RedisAdapter : RedisAdapterConsumer, IRedisAdapter
    {
        public RedisAdapter(RedisConfig config, IConnectionPoolManager connectionPoolManager) : base(config, connectionPoolManager) { }
    }
}
