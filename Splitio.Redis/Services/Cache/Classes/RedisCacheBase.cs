using Splitio.Redis.Services.Domain;

namespace Splitio.Redis.Services.Cache.Classes
{
    public abstract class RedisCacheBase
    {
        public const string KeyHashTag = "{SPLITIO}";

        private const string RedisKeyPrefixFormat = "SPLITIO/{sdk-language-version}/{instance-id}/";

        protected string RedisKeyPrefix;
        protected string UserPrefix;
        protected string SdkVersion;
        protected string MachineIp;
        protected string MachineName;

        public RedisCacheBase(RedisConfig redisConfig = null, bool clusterMode = false)
        {
            UserPrefix = redisConfig.RedisUserPrefix;
            RedisKeyPrefix = "SPLITIO.";
            UpdateRedisKeyPrefix(UserPrefix, redisConfig, clusterMode);
        }

        public RedisCacheBase(string machineIP,
            string sdkVersion,
            string machineName,
            RedisConfig redisConfig = null, bool clusterMode = false)
        {
            UserPrefix = redisConfig.RedisUserPrefix;
            MachineIp = machineIP;
            SdkVersion = sdkVersion;
            MachineName = machineName;

            RedisKeyPrefix = RedisKeyPrefixFormat
                .Replace("{sdk-language-version}", sdkVersion)
                .Replace("{instance-id}", machineIP);
            UpdateRedisKeyPrefix(UserPrefix, redisConfig, clusterMode);
        }

        private void UpdateRedisKeyPrefix(string userPrefix, RedisConfig redisConfig, bool clusterMode)
        {
            if (!string.IsNullOrEmpty(userPrefix))
            {
                if (clusterMode)
                {
                    if (redisConfig.ClusterNodes != null && !string.IsNullOrEmpty(redisConfig.ClusterNodes.KeyHashTag))
                    {
                        userPrefix = redisConfig.ClusterNodes.KeyHashTag + userPrefix;
                    }
                    else
                    {
                        userPrefix = KeyHashTag + userPrefix;
                    }
                    UserPrefix = userPrefix;
                }
                RedisKeyPrefix = userPrefix + "." + RedisKeyPrefix;
            }
        }
    }
}
