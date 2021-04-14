using Newtonsoft.Json;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;

namespace Splitio.Redis.Telemetry.Storages
{
    public class RedisTelemetryStorage : ITelemetryStorage
    {
        private readonly TimeSpan TelemetryConfigKeyTTL = new TimeSpan(1, 0, 0); // 1 hour.

        private readonly IRedisAdapter _redisAdapter;
        private readonly ISplitLogger _log;
        private readonly string _userPrefix;
        private readonly string _sdkVersion;
        private readonly string _machineIp;
        private readonly string _machineName;

        private string TelemetryLatencyKey => "{prefix}.SPLITIO.telemetry.latencies"
            .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.");

        private string TelemetryExceptionKey => "{prefix}.SPLITIO.telemetry.exceptions"
            .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.");

        private string TelemetryConfigQueueKey => "{prefix}.SPLITIO.telemetry.config"
            .Replace("{prefix}.", string.IsNullOrEmpty(_userPrefix) ? string.Empty : $"{_userPrefix}.");

        public RedisTelemetryStorage(IRedisAdapter redisAdapter,
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
            _log = log ?? WrapperAdapter.GetLogger(typeof(RedisTelemetryStorage));
        }

        #region Public Methods
        public void RecordException(MethodEnum method)
        {
            _redisAdapter.HashIncrement(TelemetryExceptionKey, $"{_sdkVersion}/{_machineName}/{_machineIp}/{method}", 1);
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

        public void RecordLatency(MethodEnum method, int bucket)
        {
            _redisAdapter.HashIncrement(TelemetryLatencyKey, $"{_sdkVersion}/{_machineName}/{_machineIp}/{method}/{bucket}", 1);
        }
        #endregion

        #region No-Op Methods
        public void RecordNonReadyUsages()
        {
            // No-Op.
        }

        public void RecordBURTimeout()
        {
            // No-Op.
        }

        public void AddTag(string tag)
        {
            // No-Op.
        }

        public void RecordImpressionsStats(ImpressionsEnum data, long count)
        {
            // No-Op.
        }

        public void RecordEventsStats(EventsEnum data, long count)
        {
            // No-Op.
        }

        public void RecordSuccessfulSync(ResourceEnum resource, long timestamp)
        {
            // No-Op.
        }

        public void RecordSyncError(ResourceEnum resuource, int status)
        {
            // No-Op.
        }

        public void RecordSyncLatency(ResourceEnum resource, int bucket)
        {
            // No-Op.
        }

        public void RecordAuthRejections()
        {
            // No-Op.
        }

        public void RecordTokenRefreshes()
        {
            // No-Op.
        }

        public void RecordStreamingEvent(StreamingEvent streamingEvent)
        {
            // No-Op.
        }

        public void RecordSessionLength(long session)
        {
            // No-Op.
        }

        public MethodLatencies PopLatencies()
        {
            throw new NotImplementedException();
        }

        public MethodExceptions PopExceptions()
        {
            throw new NotImplementedException();
        }

        public long GetNonReadyUsages()
        {
            throw new NotImplementedException();
        }

        public long GetBURTimeouts()
        {
            throw new NotImplementedException();
        }

        public long GetImpressionsStats(ImpressionsEnum data)
        {
            throw new NotImplementedException();
        }

        public long GetEventsStats(EventsEnum data)
        {
            throw new NotImplementedException();
        }

        public LastSynchronization GetLastSynchronizations()
        {
            throw new NotImplementedException();
        }

        public HTTPErrors PopHttpErrors()
        {
            throw new NotImplementedException();
        }

        public HTTPLatencies PopHttpLatencies()
        {
            throw new NotImplementedException();
        }

        public long PopAuthRejections()
        {
            throw new NotImplementedException();
        }

        public long PopTokenRefreshes()
        {
            throw new NotImplementedException();
        }

        public IList<StreamingEvent> PopStreamingEvents()
        {
            throw new NotImplementedException();
        }

        public IList<string> PopTags()
        {
            throw new NotImplementedException();
        }

        public long GetSessionLength()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
