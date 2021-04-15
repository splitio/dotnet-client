using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;

namespace Splitio.Redis.Telemetry.Storages
{
    public class RedisTelemetryEvaluationProducer : RedisTelemetryStorageBase, ITelemetryEvaluationProducer
    {
        private string TelemetryLatencyKey => "{prefix}.SPLITIO.telemetry.latencies"
            .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.");

        private string TelemetryExceptionKey => "{prefix}.SPLITIO.telemetry.exceptions"
            .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.");

        public RedisTelemetryEvaluationProducer(IRedisAdapter redisAdapter, string userPrefix, string sdkVersion, string machineIp, string machineName, ISplitLogger log = null)
            : base(redisAdapter, userPrefix, sdkVersion, machineIp, machineName, log)
        {
        }

        public void RecordException(MethodEnum method)
        {
            _redisAdapter.HashIncrement(TelemetryExceptionKey, $"{_sdkVersion}/{_machineName}/{_machineIp}/{method}", 1);
        }

        public void RecordLatency(MethodEnum method, int bucket)
        {
            _redisAdapter.HashIncrement(TelemetryLatencyKey, $"{_sdkVersion}/{_machineName}/{_machineIp}/{method}/{bucket}", 1);
        }
    }
}
