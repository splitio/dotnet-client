using Newtonsoft.Json;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Storages;

namespace Splitio.Redis.Telemetry.Storages
{
    public class RedisTelemetryInitProducer : RedisTelemetryStorageBase, ITelemetryInitProducer
    {
        private string TelemetryConfigQueueKey => "{prefix}.SPLITIO.telemetry.config"
            .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.");

        public RedisTelemetryInitProducer(IRedisAdapter redisAdapter, string userPrefix, string sdkVersion, string machineIp, string machineName, ISplitLogger log = null)
            : base(redisAdapter, userPrefix, sdkVersion, machineIp, machineName, log)
        {
        }       

        public void RecordConfigInit(Config config)
        {
            var jsonData = JsonConvert.SerializeObject(new
            {
                t = new { oM = config.OperationMode, st = config.Storage, aF = config.ActiveFactories, rF = config.RedundantActiveFactories, t = config.Tags },
                m = new { i = _machineIp, n = _machineName, s = _sdkVersion }
            });

            var result = _redisAdapter.ListRightPush(TelemetryConfigQueueKey, jsonData);

            if (result == 1)
            {
                if (!_redisAdapter.KeyExpire(TelemetryConfigQueueKey, TelemetryConfigKeyTTL))
                {
                    _log.Error("Something were wrong setting expiration");
                }
            }
        }

        public void RecordNonReadyUsages()
        {
            // No-Op
        }

        public void RecordBURTimeout()
        {
            // No-Op
        }
    }
}
