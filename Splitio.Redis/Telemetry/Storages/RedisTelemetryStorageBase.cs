using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;

namespace Splitio.Redis.Telemetry.Storages
{
    public class RedisTelemetryStorageBase
    {
        protected readonly TimeSpan TelemetryConfigKeyTTL = new TimeSpan(1, 0, 0); // 1 hour.

        protected readonly IRedisAdapter _redisAdapter;
        protected readonly ISplitLogger _log;
        protected readonly string _userPrefix;
        protected readonly string _sdkVersion;
        protected readonly string _machineIp;
        protected readonly string _machineName;        

        public RedisTelemetryStorageBase(IRedisAdapter redisAdapter,
            string userPrefix,
            string sdkVersion,
            string machineIp,
            string machineName,
            ISplitLogger log = null)
        {
            _redisAdapter = redisAdapter;
            _userPrefix = userPrefix;
            _sdkVersion = sdkVersion;
            _machineIp = machineIp;
            _machineName = machineName;
            _log = log ?? WrapperAdapter.GetLogger(typeof(RedisTelemetryStorageBase));
        }
    }
}
