using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;

namespace Splitio.Domain
{
    public static class RedisConfigurationValidator
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(RedisConfigurationValidator));

        public static void Validate(CacheAdapterConfigurationOptions config)
        {
            if (config == null || config.Type != AdapterType.Redis)
            {
                throw new ArgumentException("Redis config should be set to build split client in Consumer mode.");
            }
            if (!string.IsNullOrEmpty(config.RedisConnectionString))
            {
                if (!string.IsNullOrEmpty(config.Host) || config.RedisClusterNodes != null)
                {
                    _log.Warn("Redis Connection String is set, will ignore all other properties.");
                }
            
                config.RedisClusterNodes = new ClusterNodes();
            
                SetKeyHashTagDefault(config);
            
                return;
            }
            if ((string.IsNullOrEmpty(config.Host) || string.IsNullOrEmpty(config.Port)) && config.RedisClusterNodes == null)
            {
                throw new Exception("Redis Host and Port or Cluster Nodes should be set to initialize Split SDK in Redis Mode.");
            }

            if (config.RedisClusterNodes != null)
            {
                if (config.RedisClusterNodes.EndPoints.Count == 0)
                {
                    throw new Exception("Redis Cluster Nodes should have at least one host to initialize Split SDK in Redis Mode.");
                }

                if (string.IsNullOrEmpty(config.RedisClusterNodes.KeyHashTag))
                {
                    config.RedisClusterNodes.KeyHashTag = "{SPLITIO}";
                    _log.Warn("Redis Cluster Hashtag is not set, will set its value to: {SPLITIO}.");
                }

                if (!string.IsNullOrEmpty(config.Host))
                {
                    _log.Warn("Redis Cluster Nodes and single host are set, will default to cluster node entry.");
                }
            }
        }
    }
}
