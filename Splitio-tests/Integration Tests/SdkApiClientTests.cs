using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.CommonLibraries;
using Splitio.Services.Common;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class SdkApiClientTests
    {
        [TestMethod]
        [Ignore]
        public async Task ExecuteGetSuccessful()
        {
            //Arrange
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };

            var httpClient = new SplitioHttpClient("ABCD", 10000, 10000, headers);

            //Act
            var result = await httpClient.GetAsync("http://demo7064886.mockable.io/messages?item=msg1");

            //Assert
            Assert.AreEqual(result.statusCode, HttpStatusCode.OK);
            Assert.IsTrue(result.content.Contains("Hello World"));
        }

        [TestMethod]
        [Ignore]
        public async Task ExecuteGetShouldReturnErrorNotAuthorized()
        {
            //Arrange
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };

            var httpClient = new SplitioHttpClient(string.Empty, 10000, 10000, headers);

            //Act
            var result = await httpClient.GetAsync("http://demo7064886.mockable.io/messages?item=msg2");

            //Assert
            Assert.AreEqual(result.statusCode, HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        [Ignore]
        public async Task ExecuteGetShouldReturnNotFoundOnInvalidRequest()
        {
            //Arrange
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };

            var httpClient = new SplitioHttpClient("ABCD", 10000, 10000, headers);

            //Act
            var result = await httpClient.GetAsync("http://demo706abcd.mockable.io/messages?item=msg2");

            //Assert
            Assert.AreEqual(result.statusCode, HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task ExecuteGetShouldReturnEmptyResponseOnInvalidURL()
        {
            //Arrange
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };

            var httpClient = new SplitioHttpClient("ABCD", 10000, 10000, headers);

            //Act
            var result = await httpClient.GetAsync("http://demo70e.iio/messages?item=msg2");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.content);
            
        }
    }
}
