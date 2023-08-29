using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.SplitFetcher.Classes;
using Splitio.Telemetry.Storages;
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
            var httpClient = new SplitioHttpClient(string.Empty, config, headers);
            var SplitSdkApiClient = new SplitSdkApiClient(httpClient, telemetryStorage, baseUrl);

            //Act
            var result = await SplitSdkApiClient.FetchSplitChangesAsync(-1, new FetchOptions());

            //Assert
            Assert.IsTrue(result == string.Empty);
        }
    }
}
