using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Commons.Dto;
using Splitio.Services.Common;
using Splitio.Commons.Engine.Filters;
using Splitio.Commons.api.Classes;
using Splitio.Commons.Telemetry.Storages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class SplitSdkApiClientTests
    {
        [TestMethod]
        public async Task ExecuteGetShouldReturnEmptyIfNotAuthorized()
        {
            //Arrange
            var baseUrl = "https://sdk.aws.staging.split.io/api";
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };

            var telemetryStorage = new InMemoryTelemetryStorage();
            var config = new SelfRefreshingConfig
            {
                HttpConnectionTimeout = 10000,
                HttpReadTimeout = 10000
            };
            var httpClient = new SplitioHttpClient(string.Empty, config.ProxyHost, config.ProxyPort, config.HttpConnectionTimeout, config.HttpReadTimeout, headers);
            var fsFilter = new FlagSetsFilter(new HashSet<string>());
            var SplitSdkApiClient = new SplitSdkApiClient(httpClient, telemetryStorage, baseUrl, fsFilter, false);

            //Act
            var result = await SplitSdkApiClient.FetchSplitChangesAsync(new FetchOptions());

            //Assert
            Assert.IsTrue(result.Content == string.Empty);
        }
    }
}
