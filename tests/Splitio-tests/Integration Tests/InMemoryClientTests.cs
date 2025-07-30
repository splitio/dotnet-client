using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Splitio.Services.Client.Classes;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class InMemoryClientTests
    {
        private readonly string rootFilePath;

        public InMemoryClientTests()
        {
            // This line is to clean the warnings.
            rootFilePath = string.Empty;

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        [DeploymentItem(@"Resources\splits_staging_3.json")]
        public void OverridingJsonConvertSettingSnakeCaseNamingStrategy()
        {
            //Arrange
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            var client = new JSONFileClient($"{rootFilePath}splits_staging_3.json", "");
            client.BlockUntilReady(1000);

            //Act           
            var result = client.GetTreatment("test", "asd", null);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("off", result);
        }
    }
}
