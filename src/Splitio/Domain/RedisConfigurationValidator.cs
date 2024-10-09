using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Domain
{
    public static class RedisConfigurationValidator
    {
        public const string DefaultHashTag = "{SPLITIO}";

        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(RedisConfigurationValidator));

        public static void Validate(CacheAdapterConfigurationOptions config)
        {
            ValidateNullAdapter(config);

            if (!string.IsNullOrEmpty(config.RedisConnectionString))
            {
                if (!string.IsNullOrEmpty(config.Host) || config.RedisClusterNodes != null)
                {
                    _log.Warn("Redis Connection String is set, will ignore all other properties.");
                }
                return;
            }

            ValidateRedisAndClusterHosts(config);

            ValidateClusterOptions(config);
        }

        private static void ValidateNullAdapter(CacheAdapterConfigurationOptions config)
        {
            if (config == null || config.Type != AdapterType.Redis)
            {
                throw new ArgumentException("Redis config should be set to build split client in Consumer mode.");
            }
        }

        private static void ValidateRedisAndClusterHosts(CacheAdapterConfigurationOptions config)
        {
            if ((string.IsNullOrEmpty(config.Host) || string.IsNullOrEmpty(config.Port)) && config.RedisClusterNodes == null)
            {
                throw new Exception("Redis Host and Port or Cluster Nodes should be set to initialize Split SDK in Redis Mode.");
            }
        }

        private static void ValidateClusterOptions(CacheAdapterConfigurationOptions config)
        {
            if (config.RedisClusterNodes != null)
            {
                if (config.RedisClusterNodes.EndPoints.Count == 0)
                {
                    throw new Exception("Redis Cluster Nodes should have at least one host to initialize Split SDK in Redis Mode.");
                }

                config.RedisClusterNodes.KeyHashTag = ValidateHashTag(config.RedisClusterNodes.KeyHashTag);
     
                if (!string.IsNullOrEmpty(config.Host))
                {
                    _log.Warn("Redis Cluster Nodes and single host are set, will default to cluster node entry.");
                }
            }
        }

        private static string ValidateHashTag(string hashTag)
        {
            if (string.IsNullOrEmpty(hashTag))
            {
                _log.Warn($"Redis Cluster Hashtag is not set, will set its value to: {DefaultHashTag}.");
                return DefaultHashTag;
            }

            if (hashTag.Count() <= 2)
            {
                _log.Warn($"Redis Cluster Hashtag length is less than 3 characters, will set its value to: {DefaultHashTag}.");
                return DefaultHashTag;
            }

            if (!hashTag.StartsWith("{") || !hashTag.EndsWith("}"))
            {
                _log.Warn($"Redis Cluster Hashtag must start wth `}}` and end with `{{` characters, will set its value to: {DefaultHashTag}.");
                return DefaultHashTag;
            }

            return hashTag;
        }
    }
}
