using Splitio.Domain;
using Splitio.Services.Client.Classes;

namespace Splitio.Redis.Services.Domain
{
    public class RedisConfig : BaseConfig
    {
        public Mode Mode { get; set; }
        public string RedisHost { get; set; }
        public string RedisPort { get; set; }
        public string RedisPassword { get; set; }
        public string RedisUserPrefix { get; set; }
        public int RedisDatabase { get; set; }
        public int RedisConnectTimeout { get; set; }
        public int RedisConnectRetry { get; set; }
        public int RedisSyncTimeout { get; set; }
        public TlsConfig TlsConfig { get; set; }
    }
}
