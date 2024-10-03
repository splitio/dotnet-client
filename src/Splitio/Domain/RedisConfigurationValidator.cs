using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;

namespace Splitio.Domain
{
    public class RedisConfigurationValidator
    {
        protected static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(RedisConfigurationValidator));

        public static void ValidateRedisOptions(ConfigurationOptions options)
        {
            if ((string.IsNullOrEmpty(options.CacheAdapterConfig.Host) || string.IsNullOrEmpty(options.CacheAdapterConfig.Port)) && options.CacheAdapterConfig.RedisClusterNodes == null)
            {
                throw new ArgumentNullException("Redis Host and Port or Cluster Nodes should be set to initialize Split SDK in Redis Mode.");
            }
            if (options.CacheAdapterConfig.RedisClusterNodes != null)
            {
                if (options.CacheAdapterConfig.RedisClusterNodes.EndPoints.Count == 0)
                {
                    throw new ArgumentNullException("Redis Cluster Nodes should have at least one host to initialize Split SDK in Redis Mode.");

                }
                if (options.CacheAdapterConfig.RedisClusterNodes.KeyHashTag == null)
                {
                    options.CacheAdapterConfig.RedisClusterNodes.KeyHashTag = "{SPLITIO}";
                    _log.Warn("Redis Cluster Hashtag is not set, will set its value to: {SPLITIO}.");
                }
                if (!string.IsNullOrEmpty(options.CacheAdapterConfig.Host))
                {
                    _log.Warn("Redis Cluster Nodes and single host are set, will default to cluster node entry.");
                }
            }
        }
    }
}
