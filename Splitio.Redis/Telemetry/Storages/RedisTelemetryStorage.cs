using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Telemetry.Domain.Enums;

namespace Splitio.Redis.Telemetry.Storages
{
    public class RedisTelemetryStorage : IRedisTelemetryStorageProducer
    {
        private const string TelemetryLatencyKey = "{prefix}.SPLITIO.telemetry.latencies.{metadata}.{method}.{bucket}";
        private const string TelemetryExceptionKey = "{prefix}.SPLITIO.telemetry.exceptions.{metadata}.{method}";

        private readonly IRedisAdapter _redisAdapter;
        private readonly string _userPrefix;
        private readonly string _sdkVersion;
        private readonly string _machineIp;
        private readonly string _machineName;

        public RedisTelemetryStorage(IRedisAdapter redisAdapter,
            string userPrefix,
            string sdkVersion,
            string machineIp,
            string machineName)
        {
            _redisAdapter = redisAdapter;
            _userPrefix = userPrefix;
            _sdkVersion = sdkVersion;
            _machineIp = machineIp;
            _machineName = machineName;
        }

        #region Public Methods
        public void RecordException(MethodEnum method)
        {
            _redisAdapter.HashIncrement(BuildKeyException(method.ToString()), 1);
        }

        public void RecordInit()
        {
            
        }

        public void RecordLatency(MethodEnum method, int bucket)
        {
            _redisAdapter.HashIncrement(BuildKeyLatency(method.ToString(), bucket), 1);
        }

        public void RecordNonReadyUsages()
        {
            throw new System.NotImplementedException();
        }

        public void RecordBURTimeout()
        {
            throw new System.NotImplementedException();
        }
        #endregion

        #region Private Methods
        private string FormatMetadata()
        {
            return $"[{_sdkVersion}.{_machineIp}.{_machineName}]";
        }

        private string BuildKeyLatency(string method, int bucket)
        {
            return TelemetryLatencyKey
                .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.")
                .Replace("{metadata}", FormatMetadata())
                .Replace("{method}", method)
                .Replace("{bucket}", bucket.ToString());
        }

        private string BuildKeyException(string method)
        {
            return TelemetryExceptionKey
                .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.")
                .Replace("{metadata}", FormatMetadata())
                .Replace("{method}", method);
        }
        #endregion
    }
}
