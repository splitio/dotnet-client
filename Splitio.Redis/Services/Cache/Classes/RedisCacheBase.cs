using Splitio.Redis.Services.Domain;
using Splitio.Domain;

namespace Splitio.Redis.Services.Cache.Classes
{
    public abstract class RedisCacheBase
    {
        protected string RedisKeyPrefix;
        protected string SdkVersion;
        protected string MachineIp;
        protected string MachineName;

        protected RedisCacheBase(RedisConfig redisConfig, bool clusterMode)
        {
            MachineIp = redisConfig.SdkMachineIP;
            SdkVersion = redisConfig.SdkVersion;
            MachineName = redisConfig.SdkMachineName;

            RedisKeyPrefix = "SPLITIO.";

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

            var hashTag = RedisConfigurationValidator.DefaultHashTag;

            if (redisConfig.ClusterNodes != null && !string.IsNullOrEmpty(redisConfig.ClusterNodes.KeyHashTag))
            {
                hashTag = redisConfig.ClusterNodes.KeyHashTag;
            }

            RedisKeyPrefix = hashTag + RedisKeyPrefix;
        }
    }
}
