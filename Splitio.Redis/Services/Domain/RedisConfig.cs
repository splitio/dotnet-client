using Splitio.Domain;
using Splitio.Redis.Services.Shared;
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
        public int PoolSize { get; set; }
        public ClusterNodes ClusterNodes { get; set; }

#if NET_LATEST
        public AsyncLocalProfiler LocalProfiler { get; set; }
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
            PoolSize = options.PoolSize ?? 1;
            ClusterNodes = options.RedisClusterNodes;
            if (RedisUserPrefix != null && ClusterNodes != null && ClusterNodes.KeyHashTag != null)
            {
                RedisUserPrefix = ClusterNodes.KeyHashTag + RedisUserPrefix;
            }

#if NET_LATEST
            if (options.ProfilingEnabled)
                LocalProfiler = new AsyncLocalProfiler();
#endif
        }
    }
}
