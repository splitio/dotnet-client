using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.Filters;
using Splitio.Services.SplitFetcher.Classes;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
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
            var splitSdkApiClient = new SplitSdkApiClient(_httpClient.Object, _telemetryRuntimeProducer.Object, baseUrl, flagSetsFilter, false);
            var expectedUrl = $"{baseUrl}/api/splitChanges?s=1.3&since=-1&rbSince=-1";

            _httpClient
                .Setup(mock => mock.GetAsync(expectedUrl, false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = "ok",
                    IsSuccessStatusCode = true
                });

            // Act.
            var result = await splitSdkApiClient.FetchSplitChangesAsync(new FetchOptions
            {
                FeatureFlagsSince = -1,
                RuleBasedSegmentsSince = -1
            });

            // Assert.
            Assert.AreEqual("ok", result.Content);
            Assert.IsTrue(result.Success);
            _httpClient.Verify(mock => mock.GetAsync(expectedUrl, false), Times.Once);
        }

        [TestMethod]
        public async Task FetchSplitChangesAsyncWithSinceAndTill()
        {
            // Arrange.
            var baseUrl = "https://app.split-testing.io";
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var splitSdkApiClient = new SplitSdkApiClient(_httpClient.Object, _telemetryRuntimeProducer.Object, baseUrl, flagSetsFilter, false);
            var expectedUrl = $"{baseUrl}/api/splitChanges?s=1.3&since=-1&rbSince=-1&till=10";

            _httpClient
                .Setup(mock => mock.GetAsync(expectedUrl, false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = "ok",
                    IsSuccessStatusCode = true
                });

            // Act.
            var result = await splitSdkApiClient.FetchSplitChangesAsync(new FetchOptions
            {
                FeatureFlagsSince = -1,
                RuleBasedSegmentsSince=-1,
                Till = 10
            });

            // Assert.
            Assert.AreEqual("ok", result.Content);
            Assert.IsTrue(result.Success);
            _httpClient.Verify(mock => mock.GetAsync(expectedUrl, false), Times.Once);
        }

        [TestMethod]
        public async Task FetchSplitChangesAsyncWithSinceAndSets()
        {
            // Arrange.
            var baseUrl = "https://app.split-testing.io";
            var sets = new HashSet<string> { "set_c", "set_a", "set_b" };
            var flagSetsFilter = new FlagSetsFilter(sets);
            var splitSdkApiClient = new SplitSdkApiClient(_httpClient.Object, _telemetryRuntimeProducer.Object, baseUrl, flagSetsFilter, false);
            var expectedUrl = $"{baseUrl}/api/splitChanges?s=1.3&since=-1&rbSince=-1&sets=set_a,set_b,set_c";

            _httpClient
                .Setup(mock => mock.GetAsync(expectedUrl, false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = "ok",
                    IsSuccessStatusCode = true
                });

            // Act.
            var result = await splitSdkApiClient.FetchSplitChangesAsync(new FetchOptions
            {
                FeatureFlagsSince = -1,
                RuleBasedSegmentsSince = -1,
            });

            // Assert.
            Assert.AreEqual("ok", result.Content);
            _httpClient.Verify(mock => mock.GetAsync(expectedUrl, false), Times.Once);
        }

        [TestMethod]
        public async Task FetchSplitChangesAsyncWithSinceAndTillAndSets()
        {
            // Arrange.
            var baseUrl = "https://app.split-testing.io";
            var sets = new HashSet<string> { "set_c", "set_a", "set_b" };
            var flagSetsFilter = new FlagSetsFilter(sets);
            var splitSdkApiClient = new SplitSdkApiClient(_httpClient.Object, _telemetryRuntimeProducer.Object, baseUrl, flagSetsFilter, false);
            var expectedUrl = $"{baseUrl}/api/splitChanges?s=1.3&since=-1&rbSince=22&sets=set_a,set_b,set_c&till=11";

            _httpClient
                .Setup(mock => mock.GetAsync(expectedUrl, false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = "ok",
                    IsSuccessStatusCode = true
                });

            // Act.
            var result = await splitSdkApiClient.FetchSplitChangesAsync(new FetchOptions
            {
                FeatureFlagsSince = -1,
                RuleBasedSegmentsSince = 22,
                Till = 11
            });

            // Assert.
            Assert.AreEqual("ok", result.Content);
            Assert.IsTrue(result.Success);
            _httpClient.Verify(mock => mock.GetAsync(expectedUrl, false), Times.Once);
        }

        [TestMethod]
        public async Task FetchSplitChangesAsync_SwitchFlagSpec_Ok()
        {
            // Arrange.
            var baseUrl = "https://app.split-testing.io";
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var splitSdkApiClient = new SplitSdkApiClient(_httpClient.Object, _telemetryRuntimeProducer.Object, baseUrl, flagSetsFilter,true);
            var expectedUrl = $"{baseUrl}/api/splitChanges?s=1.3&since=-1&rbSince=-1";

            _httpClient
                .Setup(mock => mock.GetAsync(expectedUrl, false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Content = "error",
                    IsSuccessStatusCode = false
                });

            _httpClient
                .Setup(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?s=1.1&since=-1", false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = "ok",
                    IsSuccessStatusCode = true
                });

            // Act.
            var result = await splitSdkApiClient.FetchSplitChangesAsync(new FetchOptions
            {
                FeatureFlagsSince = -1,
                RuleBasedSegmentsSince = -1                
            });

            // Assert.
            Assert.AreEqual("ok", result.Content);
            Assert.IsTrue(result.Success);
            _httpClient.Verify(mock => mock.GetAsync(expectedUrl, false), Times.Once);
            _httpClient.Verify(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?s=1.1&since=-1", false), Times.Once);
        }

        [TestMethod]
        public async Task FetchSplitChangesAsync_SwitchFlagSpec_BadRequest()
        {
            // Arrange.
            var baseUrl = "https://app.split-testing.io";
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var splitSdkApiClient = new SplitSdkApiClient(_httpClient.Object, _telemetryRuntimeProducer.Object, baseUrl, flagSetsFilter, true);
            var expectedUrl = $"{baseUrl}/api/splitChanges?s=1.3&since=-1&rbSince=-1";

            _httpClient
                .Setup(mock => mock.GetAsync(expectedUrl, false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Content = "error",
                    IsSuccessStatusCode = false
                });

            _httpClient
                .Setup(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?s=1.1&since=-1", false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Content = "error",
                    IsSuccessStatusCode = false
                });

            // Act.
            var result = await splitSdkApiClient.FetchSplitChangesAsync(new FetchOptions
            {
                FeatureFlagsSince = -1,
                RuleBasedSegmentsSince = -1
            });

            // Assert.
            Assert.AreEqual(string.Empty, result.Content);
            Assert.IsFalse(result.Success);
            _httpClient.Verify(mock => mock.GetAsync(expectedUrl, false), Times.Once);
            _httpClient.Verify(mock => mock.GetAsync($"{baseUrl}/api/splitChanges?s=1.1&since=-1", false), Times.Once);
        }

        [TestMethod]
        public async Task FetchSplitChangesAsync_SwitchFlagSpecAndBack_Ok()
        {
            // Arrange.
            var baseUrl = "https://app.split-testing.io";
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var splitSdkApiClient = new SplitSdkApiClient(_httpClient.Object, _telemetryRuntimeProducer.Object, baseUrl, flagSetsFilter, true, 100);
            var expectedUrl1_3 = $"{baseUrl}/api/splitChanges?s=1.3&since=-1&rbSince=-1";
            var expectedUrl1_1 = $"{baseUrl}/api/splitChanges?s=1.1&since=-1";

            _httpClient
                .SetupSequence(mock => mock.GetAsync(expectedUrl1_3, false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Content = "error",
                    IsSuccessStatusCode = false
                })
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = "ok",
                    IsSuccessStatusCode = true
                });

            _httpClient
                .Setup(mock => mock.GetAsync(expectedUrl1_1, false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = "ok",
                    IsSuccessStatusCode = true
                });

            // Act and Assert.
            var result = await splitSdkApiClient.FetchSplitChangesAsync(new FetchOptions
            {
                FeatureFlagsSince = -1,
                RuleBasedSegmentsSince = -1
            });

            Assert.AreEqual("ok", result.Content);
            Assert.IsTrue(result.Success);
            _httpClient.Verify(mock => mock.GetAsync(expectedUrl1_1, false), Times.Once);
            _httpClient.Verify(mock => mock.GetAsync(expectedUrl1_3, false), Times.Once);


            Thread.Sleep(150);
            result = await splitSdkApiClient.FetchSplitChangesAsync(new FetchOptions
            {
                FeatureFlagsSince = -1,
                RuleBasedSegmentsSince = -1
            });

            Assert.AreEqual("ok", result.Content);
            Assert.IsTrue(result.Success);
            _httpClient.Verify(mock => mock.GetAsync(expectedUrl1_1, false), Times.Once);
            _httpClient.Verify(mock => mock.GetAsync(expectedUrl1_3, false), Times.Exactly(2));
        }
    }
}
