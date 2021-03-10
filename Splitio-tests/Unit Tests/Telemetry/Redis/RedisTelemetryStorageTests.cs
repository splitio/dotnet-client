using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Redis.Services.Cache.Interfaces;
using Splitio.Redis.Telemetry.Storages;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;

namespace Splitio_Tests.Unit_Tests.Telemetry.Redis
{
    [TestClass]
    public class RedisTelemetryStorageTests
    {
        private Mock<IRedisAdapter> _redisAdapter;
        private ITelemetryStorage _telemetryStorage;

        [TestInitialize]
        public void Initialization()
        {
            _redisAdapter = new Mock<IRedisAdapter>();
        }

        [TestMethod]
        public void RecordExceptionTest()
        {
            // Arrange.
            _telemetryStorage = new RedisTelemetryStorage(_redisAdapter.Object, "user-prefix", "sdk-test-version", "10.0.0.0", "machine-name-test");

            // Act & Assert.
            _telemetryStorage.RecordException(MethodEnum.Treatment);
            _telemetryStorage.RecordException(MethodEnum.Treatment);
            _telemetryStorage.RecordException(MethodEnum.Treatments);
            _telemetryStorage.RecordException(MethodEnum.TreatmentsWithConfig);
        }
    }
}
