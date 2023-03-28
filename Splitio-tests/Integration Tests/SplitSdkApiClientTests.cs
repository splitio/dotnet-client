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
            var httpClient = new SplitioHttpClient(string.Empty, 10000, 10000, headers);
            var SplitSdkApiClient = new SplitSdkApiClient(httpClient, telemetryStorage, baseUrl);

            //Act
            var result = await SplitSdkApiClient.FetchSplitChanges(-1, new FetchOptions());

            //Assert
            Assert.IsTrue(result == string.Empty);
        }
    }
}
