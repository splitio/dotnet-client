using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Telemetry.Storages;
using Splitio.Services.Client.Classes;
using Splitio.Services.Logger;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;

namespace Splitio_Tests.Unit_Tests.Telemetry.Storages
{
    [TestClass]
    public class RedisTelemetryStorageTests
    {
        private Mock<IRedisAdapter> _redisAdapter;
        private Mock<ISplitLogger> _log;
        private string _userPrefix;
        private string _sdkVersion;
        private string _machineIp;
        private string _machineName;

        private RedisTelemetryStorage _telemetryStorage;

        [TestInitialize]
        public void Initialization()
        {
            _redisAdapter = new Mock<IRedisAdapter>();
            _log = new Mock<ISplitLogger>();
            _userPrefix = "user-prefix-test";
            _sdkVersion = "sdk-version-test";
            _machineIp = "10.0.0.1";
            _machineName = "machine-name-test";

            _telemetryStorage = new RedisTelemetryStorage(_redisAdapter.Object, _userPrefix, _sdkVersion, _machineIp, _machineName, _log.Object);
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
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.exceptions", $"{_sdkVersion}/{_machineName}/{_machineIp}/Track", 1), Times.Once);
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.exceptions", $"{_sdkVersion}/{_machineName}/{_machineIp}/Treatment", 1), Times.Exactly(2));
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.exceptions", $"{_sdkVersion}/{_machineName}/{_machineIp}/TreatmentsWithConfig", 1), Times.Once);
        }

        [TestMethod]
        public void RecordConfigInitShouldRecordConfigAndSetExpirationTime()
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

            var redisValue = JsonConvert.SerializeObject(new
            {
                t = new { oM = config.OperationMode, st = config.Storage, aF = config.ActiveFactories, rF = config.RedundantActiveFactories, t = config.Tags },
                m = new { i = _machineIp, n = _machineName, s = _sdkVersion }
            });
            var key = $"{_userPrefix}.SPLITIO.telemetry.config";

            _redisAdapter
                .Setup(mock => mock.ListRightPush(key, redisValue))
                .Returns(1);

            // Act.
            _telemetryStorage.RecordConfigInit(config);

            // Assert.
            _redisAdapter.Verify(mock => mock.ListRightPush(key, redisValue), Times.Once);
            _redisAdapter.Verify(mock => mock.KeyExpire(key, It.IsAny<TimeSpan>()), Times.Once);
        }

        [TestMethod]
        public void RecordConfigInitShouldOnlyRecordConfig()
        {
            // Assert.
            var config = new Config
            {
                ActiveFactories = 2,
                EventsQueueSize = 1,
                OperationMode = (int)Mode.Consumer,
                ImpressionsMode = ImpressionsMode.Optimized,
                BURTimeouts = 5
            };

            var redisValue = JsonConvert.SerializeObject(new
            {
                t = new { oM = config.OperationMode, st = config.Storage, aF = config.ActiveFactories, rF = config.RedundantActiveFactories, t = config.Tags },
                m = new { i = _machineIp, n = _machineName, s = _sdkVersion }
            });
            var key = $"{_userPrefix}.SPLITIO.telemetry.config";

            _redisAdapter
                .Setup(mock => mock.ListRightPush(key, redisValue))
                .Returns(2);

            // Act.
            _telemetryStorage.RecordConfigInit(config);

            // Assert.
            _redisAdapter.Verify(mock => mock.ListRightPush(key, redisValue), Times.Once);
            _redisAdapter.Verify(mock => mock.KeyExpire(key, It.IsAny<TimeSpan>()), Times.Never);
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
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.latencies", $"{_sdkVersion}/{_machineName}/{_machineIp}/Track/1", 1), Times.Once);
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.latencies", $"{_sdkVersion}/{_machineName}/{_machineIp}/Track/2", 1), Times.Once);
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.latencies", $"{_sdkVersion}/{_machineName}/{_machineIp}/Treatments/3", 1), Times.Once);
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.latencies", $"{_sdkVersion}/{_machineName}/{_machineIp}/TreatmentsWithConfig/4", 1), Times.Once);
            _redisAdapter.Verify(mock => mock.HashIncrement($"{_userPrefix}.SPLITIO.telemetry.latencies", $"{_sdkVersion}/{_machineName}/{_machineIp}/TreatmentWithConfig/5", 1), Times.Once);
        }
    }
}
