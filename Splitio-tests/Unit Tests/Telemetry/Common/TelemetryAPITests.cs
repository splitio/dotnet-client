using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.CommonLibraries;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Domain;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Telemetry.Common
{
    [TestClass]
    public class TelemetryAPITests
    {
        private Mock<ISplitioHttpClient> _splitioHttpClient;
        private Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;

        private ITelemetryAPI _telemetryAPI;

        [TestInitialize]
        public void Initialization()
        {
            _splitioHttpClient = new Mock<ISplitioHttpClient>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();

            _telemetryAPI = new TelemetryAPI(_splitioHttpClient.Object, "www.fake-url.com", _telemetryRuntimeProducer.Object);
        }

        [TestMethod]
        public async Task RecordConfigInit()
        {
            // Arrange.
            var config = new Config
            {
                ActiveFactories = 1,
                BURTimeouts = 2,
                EventsQueueSize = 3,
                OperationMode = (int)Mode.Consumer,
                Rates = new Rates
                {
                    Events = 1,
                    Impressions = 2,
                    Segments = 3,
                    Splits = 4
                }
            };

            var data = "{\"oM\":1,\"sE\":false,\"rR\":{\"sp\":4,\"se\":3,\"im\":2,\"ev\":1,\"te\":0},\"iQ\":0,\"eQ\":3,\"iM\":0,\"iL\":false,\"hp\":false,\"aF\":1,\"rF\":0,\"tR\":0,\"bT\":2,\"nR\":0}";

            _splitioHttpClient
                .Setup(mock => mock.PostAsync("www.fake-url.com/metrics/config", data))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK
                });

            _telemetryRuntimeProducer
                .Setup(mock => mock.RecordSuccessfulSync(It.IsAny<ResourceEnum>(), It.IsAny<long>()));

            // Act.
            await _telemetryAPI.RecordConfigInitAsync(config);

            // Assert.
            _splitioHttpClient.Verify(mock => mock.PostAsync("www.fake-url.com/metrics/config", data), Times.Once);
        }

        [TestMethod]
        public async Task RecordStats()
        {
            // Arrange.
            var stats = new Stats
            {
                AuthRejections = 2,
                HTTPLatencies = new HTTPLatencies
                {
                    Events = new List<long> { 55, 66, 77 },
                    Segments = new List<long> { 88, 22, 99 },
                },
                HTTPErrors = new HTTPErrors
                {
                    Splits = new Dictionary<int, long> { { 500, 5 }, { 400, 5 } },
                    Events = new Dictionary<int, long> { { 500, 2 }, { 400, 2 } }
                }
            };

            var data = "{\"hE\":{\"sp\":{\"500\":5,\"400\":5},\"ev\":{\"500\":2,\"400\":2}},\"hL\":{\"se\":[88,22,99],\"ev\":[55,66,77]},\"tR\":0,\"aR\":2,\"iQ\":0,\"iDe\":0,\"iDr\":0,\"spC\":0,\"seC\":0,\"skC\":0,\"sL\":0,\"eQ\":0,\"eD\":0}";

            _splitioHttpClient
                .Setup(mock => mock.PostAsync("www.fake-url.com/metrics/usage", data))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK
                });

            // Act.
            await _telemetryAPI.RecordStatsAsync(stats);

            // Assert.
            _splitioHttpClient.Verify(mock => mock.PostAsync("www.fake-url.com/metrics/usage", data), Times.Once);
        }

        [TestMethod]
        public async Task RecordUniqueKeys()
        {
            // Arrange.
            var values = new List<Mtks> { new Mtks("feature-01", new HashSet<string> { "key-01", "key-02", "key-03", "key-04" }) };

            var uniqueKeys = new UniqueKeys(values);

            var data = "{\"keys\":[{\"f\":\"feature-01\",\"ks\":[\"key-01\",\"key-02\",\"key-03\",\"key-04\"]}]}";
            _splitioHttpClient
                .Setup(mock => mock.PostAsync("www.fake-url.com/keys/ss", data))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK
                });

            // Act.
            await _telemetryAPI.RecordUniqueKeysAsync(uniqueKeys);

            // Assert.
            _splitioHttpClient.Verify(mock => mock.PostAsync("www.fake-url.com/keys/ss", data), Times.Once);
        }
    }
}
