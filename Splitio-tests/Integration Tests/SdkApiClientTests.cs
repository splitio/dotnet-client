using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.CommonLibraries;
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
            var baseUrl = "http://demo7064886.mockable.io";
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };

            var SdkApiClient = new SdkApiClient("ABCD", headers, baseUrl, 10000, 10000);

            //Act
            var result = await SdkApiClient.ExecuteGet("/messages?item=msg1");

            //Assert
            Assert.AreEqual(result.statusCode, HttpStatusCode.OK);
            Assert.IsTrue(result.content.Contains("Hello World"));
        }

        [TestMethod]
        [Ignore]
        public async Task ExecuteGetShouldReturnErrorNotAuthorized()
        {
            //Arrange
            var baseUrl = "http://demo7064886.mockable.io";
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };
            var SdkApiClient = new SdkApiClient(string.Empty, headers, baseUrl, 10000, 10000);

            //Act
            var result = await SdkApiClient.ExecuteGet("/messages?item=msg2");

            //Assert
            Assert.AreEqual(result.statusCode, HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        [Ignore]
        public async Task ExecuteGetShouldReturnNotFoundOnInvalidRequest()
        {
            //Arrange
            var baseUrl = "http://demo706abcd.mockable.io";
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };

            var SdkApiClient = new SdkApiClient("ABCD", headers, baseUrl, 10000, 10000);

            //Act
            var result = await SdkApiClient.ExecuteGet("/messages?item=msg2");

            //Assert
            Assert.AreEqual(result.statusCode, HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task ExecuteGetShouldReturnEmptyResponseOnInvalidURL()
        {
            //Arrange
            var baseUrl = "http://demo70e.iio";
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };
            var SdkApiClient = new SdkApiClient("ABCD", headers, baseUrl, 10000, 10000);

            //Act
            var result = await SdkApiClient.ExecuteGet("/messages?item=msg2");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.content);
            
        }
    }
}
