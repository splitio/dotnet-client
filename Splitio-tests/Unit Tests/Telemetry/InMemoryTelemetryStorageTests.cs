using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;

namespace Splitio_Tests.Unit_Tests.Telemetry
{
    [TestClass]
    public class InMemoryTelemetryStorageTests
    {
        private ITelemetryStorage _telemetryStorage;

        [TestInitialize]
        public void Initialization()
        {
            _telemetryStorage = new InMemoryTelemetryStorage();
        }

        [TestMethod]
        public void MethodLatenciesRecordAndPop()
        {
            // Arrange.
            _telemetryStorage.RecordLatency(MethodEnum.Treatment, 2343);
            _telemetryStorage.RecordLatency(MethodEnum.Treatment, 2344);
            _telemetryStorage.RecordLatency(MethodEnum.Treatments, 2345);
            _telemetryStorage.RecordLatency(MethodEnum.TreatmentsWithConfig, 2346);
            _telemetryStorage.RecordLatency(MethodEnum.Track, 78787);
            _telemetryStorage.RecordLatency(MethodEnum.Track, 678678);

            // Act.
            var result = _telemetryStorage.PopLatencies();

            // Assert.
            Assert.AreEqual(2, result.Treatment.Count);
            Assert.AreEqual(0, result.TreatmentWithConfig.Count);
            Assert.AreEqual(1, result.Treatments.Count);
            Assert.AreEqual(1, result.TreatmenstWithConfig.Count);
            Assert.AreEqual(2, result.Track.Count);

            result = _telemetryStorage.PopLatencies();
            Assert.AreEqual(0, result.Treatment.Count);
            Assert.AreEqual(0, result.TreatmentWithConfig.Count);
            Assert.AreEqual(0, result.Treatments.Count);
            Assert.AreEqual(0, result.TreatmenstWithConfig.Count);
            Assert.AreEqual(0, result.Track.Count);
        }
    }
}
