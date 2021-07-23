using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
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
        [Ignore]
        public async Task ExecuteFetchSplitChangesSuccessful()
        {
            //Arrange
            var baseUrl = "http://sdk-aws-staging.split.io/api/";
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };

            var telemetryStorage = new InMemoryTelemetryStorage();
            var SplitSdkApiClient = new SplitSdkApiClient("///PUT API KEY HERE///", headers, baseUrl, 10000, 10000, telemetryStorage);

            //Act
            var result = await SplitSdkApiClient.FetchSplitChanges(-1, new FetchOptions());
  
            //Assert
            Assert.IsTrue(result.Contains("splits"));            
        }

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
            var SplitSdkApiClient = new SplitSdkApiClient(string.Empty, headers, baseUrl, 10000, 10000, telemetryStorage);

            //Act
            var result = await SplitSdkApiClient.FetchSplitChanges(-1, new FetchOptions());

            //Assert
            Assert.IsTrue(result == string.Empty);
        }
    }
}
