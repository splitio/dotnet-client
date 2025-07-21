using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Services.Domain;
using Splitio.Redis.Telemetry.Storages;
using Splitio.Services.Client.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Telemetry.Storages
{
    [TestClass]
    public class RedisTelemetryStorageTests
    {
        private Mock<IRedisAdapterProducer> _redisAdapter;
        private string _userPrefix;
        private string _sdkVersion;
        private string _machineIp;
        private string _machineName;

        private RedisTelemetryStorage _telemetryStorage;

        [TestInitialize]
        public void Initialization()
        {
            _redisAdapter = new Mock<IRedisAdapterProducer>();
            _userPrefix = "user-prefix-test";
            _sdkVersion = "sdk-version-test";
            _machineIp = "10.0.0.1";
            _machineName = "machine-name-test";
            var config = new RedisConfig
            {
                RedisHost = "localhost",
                RedisPort = "6379",
                RedisPassword = "",
                RedisDatabase = 0,
                RedisConnectTimeout = 1000,
                RedisConnectRetry = 5,
                RedisSyncTimeout = 1000,
                RedisUserPrefix = _userPrefix,
                PoolSize = 1,
                SdkVersion = _sdkVersion,
                SdkMachineIP = _machineIp,
                SdkMachineName = _machineName,
            };

            _telemetryStorage = new RedisTelemetryStorage(_redisAdapter.Object, config, false);
        }

        [TestMethod]
        public void RecordException()
        {
            // Act.
            _telemetryStorage.RecordException(MethodEnum.Track);
            _telemetryStorage.RecordException(MethodEnum.Treatment);
            _telemetryStorage.RecordException(MethodEnum.Treatment);
            _telemetryStorage.RecordException(MethodEnum.TreatmentsWithConfig);

            // Assert.
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.exceptions", $"{_sdkVersion}/{_machineName}/{_machineIp}/track", 1), Times.Once);
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.exceptions", $"{_sdkVersion}/{_machineName}/{_machineIp}/treatment", 1), Times.Exactly(2));
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.exceptions", $"{_sdkVersion}/{_machineName}/{_machineIp}/treatmentsWithConfig", 1), Times.Once);
        }

        [TestMethod]
        public async Task RecordConfigInitShouldRecordConfigAndSetExpirationTime()
        {
            // Assert.
            var config = new Config
            {
                ActiveFactories = 2,
                EventsQueueSize = 1,
                OperationMode = (int) Mode.Consumer,
                ImpressionsMode = ImpressionsMode.Optimized,
                BURTimeouts = 5
            };

            var redisValue = JsonConvertWrapper.SerializeObject(new
            {
                t = new { oM = config.OperationMode, st = config.Storage, aF = config.ActiveFactories, rF = config.RedundantActiveFactories, t = config.Tags },
                m = new { i = _machineIp, n = _machineName, s = _sdkVersion }
            });
            var key = $"{_userPrefix}.SPLITIO.telemetry.init";

            _redisAdapter
                .Setup(mock => mock.ListRightPush(key, redisValue))
                .Returns(1);

            // Act.
            await _telemetryStorage.RecordConfigInitAsync(config);

            // Assert.
            _redisAdapter.Verify(mock => mock.HashSetAsync(key, $"{_sdkVersion}/{_machineName}/{_machineIp}", redisValue), Times.Once);
        }

        [TestMethod]
        public void RecordLatency()
        {
            // Act.
            _telemetryStorage.RecordLatency(MethodEnum.Track, 1);
            _telemetryStorage.RecordLatency(MethodEnum.Track, 2);
            _telemetryStorage.RecordLatency(MethodEnum.Treatments, 3);
            _telemetryStorage.RecordLatency(MethodEnum.TreatmentsWithConfig, 4);
            _telemetryStorage.RecordLatency(MethodEnum.TreatmentWithConfig, 5);

            // Assert.
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.latencies", $"{_sdkVersion}/{_machineName}/{_machineIp}/track/1", 1), Times.Once);
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.latencies", $"{_sdkVersion}/{_machineName}/{_machineIp}/track/2", 1), Times.Once);
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.latencies", $"{_sdkVersion}/{_machineName}/{_machineIp}/treatments/3", 1), Times.Once);
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.latencies", $"{_sdkVersion}/{_machineName}/{_machineIp}/treatmentsWithConfig/4", 1), Times.Once);
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.latencies", $"{_sdkVersion}/{_machineName}/{_machineIp}/treatmentWithConfig/5", 1), Times.Once);
        }
    }
}
