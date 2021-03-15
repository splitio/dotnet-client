using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;

namespace Splitio.Redis.Telemetry.Storages
{
    public class RedisTelemetryStorage : ITelemetryStorage
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

        public void RecordLatency(MethodEnum method, int bucket)
        {
            _redisAdapter.HashIncrement(BuildKeyLatency(method.ToString(), bucket), 1);
        }
        #endregion

        #region Not Implemented Methods
        public void RecordSyncLatency(ResourceEnum resource, int bucket)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void AddTag(string tag)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long GetBURTimeouts()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long GetEventsStats(EventsEnum data)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long GetImpressionsStats(ImpressionsEnum data)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public LastSynchronization GetLastSynchronizations()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long GetNonReadyUsages()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long GetSessionLength()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long PopAuthRejections()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public MethodExceptions PopExceptions()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public HTTPErrors PopHttpErrors()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public HTTPLatencies PopHttpLatencies()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public MethodLatencies PopLatencies()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public IList<StreamingEvent> PopStreamingEvents()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public IList<string> PopTags()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public long PopTokenRefreshes()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordAuthRejections()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordBURTimeout()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordEventsStats(EventsEnum data, long count)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }        

        public void RecordImpressionsStats(ImpressionsEnum data, long count)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }        

        public void RecordNonReadyUsages()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordSessionLength(long session)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordStreamingEvent(StreamingEvent streamingEvent)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordSuccessfulSync(ResourceEnum resource, long timestamp)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }

        public void RecordSyncError(ResourceEnum resuource, int status)
        {
            throw new System.NotImplementedException("Not implemented for redis.");
        }        

        public void RecordTokenRefreshes()
        {
            throw new System.NotImplementedException("Not implemented for redis.");
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
