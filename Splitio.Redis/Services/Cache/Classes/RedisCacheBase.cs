using Splitio.Redis.Services.Domain;

namespace Splitio.Redis.Services.Cache.Classes
{
    public abstract class RedisCacheBase
    {
        private const string KeyHashTag = "{SPLITIO}";
        private const string RedisKeyPrefixFormat = "SPLITIO/{sdk-language-version}/{instance-id}/";

        protected string RedisKeyPrefix;
        protected string SdkVersion;
        protected string MachineIp;
        protected string MachineName;

        protected RedisCacheBase(RedisConfig redisConfig, bool clusterMode, bool withMetadata)
        {
            MachineIp = redisConfig.SdkMachineIP;
            SdkVersion = redisConfig.SdkVersion;
            MachineName = redisConfig.SdkMachineName;

            if (withMetadata)
            {
                RedisKeyPrefix = RedisKeyPrefixFormat
                .Replace("{sdk-language-version}", redisConfig.SdkVersion)
                .Replace("{instance-id}", redisConfig.SdkMachineIP);
            }
            else
            {
                RedisKeyPrefix = "SPLITIO.";
            }

            SetRedisKeyPrefix(redisConfig);
            SetHashTagIfIsClusterMode(redisConfig, clusterMode);
        }

        private void SetRedisKeyPrefix(RedisConfig redisConfig)
        {
            if (string.IsNullOrEmpty(redisConfig.RedisUserPrefix)) return;
            
            RedisKeyPrefix = redisConfig.RedisUserPrefix + "." + RedisKeyPrefix;
        }

        private void SetHashTagIfIsClusterMode(RedisConfig redisConfig, bool clusterMode)
        {
            if (!clusterMode) return;

            var hashTag = KeyHashTag;

            if (redisConfig.ClusterNodes != null && !string.IsNullOrEmpty(redisConfig.ClusterNodes.KeyHashTag))
            {
                hashTag = redisConfig.ClusterNodes.KeyHashTag;
            }

            RedisKeyPrefix = hashTag + RedisKeyPrefix;
        }
    }
}
