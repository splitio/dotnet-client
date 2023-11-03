using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.Filters;
using Splitio.Services.SplitFetcher.Classes;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.SplitFetcher
{
    [TestClass]
    public class SplitSdkApiClientTests
    {
        private readonly Mock<ISplitioHttpClient> _httpClient;
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;

        public SplitSdkApiClientTests()
        {
            _httpClient = new Mock<ISplitioHttpClient>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
        }

        [TestMethod]
        public async Task FetchSplitChangesAsyncWithSince()
        {
            // Arrange.
            var baseUrl = "https://app.split-testing.io";
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var splitSdkApiClient = new SplitSdkApiClient(_httpClient.Object, _telemetryRuntimeProducer.Object, baseUrl, flagSetsFilter);

            _httpClient
                .Setup(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?since=-1", false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = "ok",
                    IsSuccessStatusCode = true
                });

            // Act.
            var result = await splitSdkApiClient.FetchSplitChangesAsync(-1, new FetchOptions());

            // Assert.
            Assert.AreEqual("ok", result);
            _httpClient.Verify(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?since=-1", false), Times.Once);
        }

        [TestMethod]
        public async Task FetchSplitChangesAsyncWithSinceAndTill()
        {
            // Arrange.
            var baseUrl = "https://app.split-testing.io";
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var splitSdkApiClient = new SplitSdkApiClient(_httpClient.Object, _telemetryRuntimeProducer.Object, baseUrl, flagSetsFilter);

            _httpClient
                .Setup(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?since=-1&till=10", false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = "ok",
                    IsSuccessStatusCode = true
                });

            // Act.
            var result = await splitSdkApiClient.FetchSplitChangesAsync(-1, new FetchOptions { Till = 10 });

            // Assert.
            Assert.AreEqual("ok", result);
            _httpClient.Verify(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?since=-1&till=10", false), Times.Once);
        }

        [TestMethod]
        public async Task FetchSplitChangesAsyncWithSinceAndSets()
        {
            // Arrange.
            var baseUrl = "https://app.split-testing.io";
            var sets = new HashSet<string> { "set_c", "set_a", "set_b" };
            var flagSetsFilter = new FlagSetsFilter(sets);
            var splitSdkApiClient = new SplitSdkApiClient(_httpClient.Object, _telemetryRuntimeProducer.Object, baseUrl, flagSetsFilter);

            _httpClient
                .Setup(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?since=-1&sets=set_a,set_b,set_c", false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = "ok",
                    IsSuccessStatusCode = true
                });

            // Act.
            var result = await splitSdkApiClient.FetchSplitChangesAsync(-1, new FetchOptions());

            // Assert.
            Assert.AreEqual("ok", result);
            _httpClient.Verify(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?since=-1&sets=set_a,set_b,set_c", false), Times.Once);
        }

        [TestMethod]
        public async Task FetchSplitChangesAsyncWithSinceAndTillAndSets()
        {
            // Arrange.
            var baseUrl = "https://app.split-testing.io";
            var sets = new HashSet<string> { "set_c", "set_a", "set_b" };
            var flagSetsFilter = new FlagSetsFilter(sets);
            var splitSdkApiClient = new SplitSdkApiClient(_httpClient.Object, _telemetryRuntimeProducer.Object, baseUrl, flagSetsFilter);

            _httpClient
                .Setup(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?since=-1&till=11&sets=set_a,set_b,set_c", false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = "ok",
                    IsSuccessStatusCode = true
                });

            // Act.
            var result = await splitSdkApiClient.FetchSplitChangesAsync(-1, new FetchOptions { Till = 11 });

            // Assert.
            Assert.AreEqual("ok", result);
            _httpClient.Verify(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?since=-1&till=11&sets=set_a,set_b,set_c", false), Times.Once);
        }
    }
}
