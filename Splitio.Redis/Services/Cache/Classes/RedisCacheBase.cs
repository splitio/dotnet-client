namespace Splitio.Redis.Services.Cache.Classes
{
    public abstract class RedisCacheBase
    {
        private const string RedisKeyPrefixFormat = "SPLITIO/{sdk-language-version}/{instance-id}/";

        protected string RedisKeyPrefix;
        protected string UserPrefix;
        protected string SdkVersion;
        protected string MachineIp;
        protected string MachineName;

        public RedisCacheBase(string userPrefix = null)
        {
            UserPrefix = userPrefix;
            RedisKeyPrefix = "SPLITIO.";

            if (!string.IsNullOrEmpty(userPrefix))
            {
                RedisKeyPrefix = userPrefix + "." + RedisKeyPrefix;
            }
        }

        public RedisCacheBase(string machineIP,
            string sdkVersion,
            string machineName,
            string userPrefix = null)
        {
            UserPrefix = userPrefix;
            MachineIp = machineIP;
            SdkVersion = sdkVersion;
            MachineName = machineName;

            RedisKeyPrefix = RedisKeyPrefixFormat
                .Replace("{sdk-language-version}", sdkVersion)
                .Replace("{instance-id}", machineIP);

            if (!string.IsNullOrEmpty(userPrefix))
            {
                RedisKeyPrefix = userPrefix + "." + RedisKeyPrefix;
            }
        }
    }
}
