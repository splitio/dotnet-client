using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class SdkApiClientTests
    {
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
            var config = new SelfRefreshingConfig
            {
                HttpConnectionTimeout = 10000,
                HttpReadTimeout = 10000
            };

            var httpClient = new SplitioHttpClient("ABCD", config, headers);

            //Act
            var result = await httpClient.GetAsync("http://demo70e.iio/messages?item=msg2");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.Content);
            
        }
    }
}
