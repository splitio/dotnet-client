using Splitio.Redis.Services.Cache.Classes;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Threading.Tasks;

namespace Splitio.Redis.Telemetry.Storages
{
    public class RedisTelemetryStorage : RedisCacheBase, ITelemetryInitProducer, ITelemetryEvaluationProducer
    {
        private readonly IRedisAdapterProducer _redisAdapterProducer;

        private string TelemetryLatencyKey => $"{RedisKeyPrefix}telemetry.latencies";
        private string TelemetryExceptionKey => $"{RedisKeyPrefix}telemetry.exceptions";
        private string TelemetryInitKey => $"{RedisKeyPrefix}telemetry.init";

        public RedisTelemetryStorage(IRedisAdapterProducer redisAdapterProducer, RedisConfig redisConfig, bool clusterMode) : base(redisConfig, clusterMode)
        {
            _redisAdapterProducer = redisAdapterProducer;
        }

        public async Task RecordConfigInitAsync(Config config)
        {
            var jsonData = JsonConvertWrapper.SerializeObject(new
            {
                t = new { oM = config.OperationMode, st = config.Storage, aF = config.ActiveFactories, rF = config.RedundantActiveFactories, t = config.Tags },
                m = new { i = MachineIp, n = MachineName, s = SdkVersion }
            });

            await _redisAdapterProducer.HashSetAsync(TelemetryInitKey, $"{SdkVersion}/{MachineName}/{MachineIp}", jsonData);
        }

        public void RecordLatency(MethodEnum method, int bucket)
        {
            _redisAdapterProducer.HashIncrement(TelemetryLatencyKey, $"{SdkVersion}/{MachineName}/{MachineIp}/{method.GetString()}/{bucket}", 1);
        }

        public async Task RecordLatencyAsync(MethodEnum method, int bucket)
        {
            await _redisAdapterProducer.HashIncrementAsync(TelemetryLatencyKey, $"{SdkVersion}/{MachineName}/{MachineIp}/{method.GetString()}/{bucket}", 1);
        }

        public void RecordException(MethodEnum method)
        {
            _redisAdapterProducer.HashIncrement(TelemetryExceptionKey, $"{SdkVersion}/{MachineName}/{MachineIp}/{method.GetString()}", 1);
        }

        public async Task RecordExceptionAsync(MethodEnum method)
        {
            await _redisAdapterProducer.HashIncrementAsync(TelemetryExceptionKey, $"{SdkVersion}/{MachineName}/{MachineIp}/{method.GetString()}", 1);
        }

        public void RecordNonReadyUsages()
        {
            // No-Op.
        }

        public void RecordBURTimeout()
        {
            // No-Op.
        }
    }
}
