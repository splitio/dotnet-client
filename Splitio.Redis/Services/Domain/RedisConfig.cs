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
#if NET_LATEST
        public bool ProfilingEnabled { get; set; }
#endif

        public string HostAndPort => $"{RedisHost}:{RedisPort}";

        public void FromCacheAdapterConfig(CacheAdapterConfigurationOptions options)
        {
            RedisHost = options.Host;
            RedisPort = options.Port;
            RedisPassword = options.Password;
            RedisDatabase = options.Database ?? 0;
            RedisConnectTimeout = options.ConnectTimeout ?? 0;
            RedisSyncTimeout = options.SyncTimeout ?? 0;
            RedisConnectRetry = options.ConnectRetry ?? 0;
            RedisUserPrefix = options.UserPrefix;
            TlsConfig = options.TlsConfig;
#if NET_LATEST
            ProfilingEnabled = options.ProfilingEnabled;
#endif
        }
    }
}
