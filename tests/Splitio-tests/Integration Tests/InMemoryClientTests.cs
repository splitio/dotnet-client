using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
using Splitio.Services.Impressions.Classes;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class InMemoryClientTests
    {
        private readonly string rootFilePath;
        private readonly FallbackTreatmentCalculator _fallbackTreatmentCalculator;
        private readonly EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> _eventsManager;

        public InMemoryClientTests()
        {
            // This line is to clean the warnings.
            rootFilePath = string.Empty;
            _fallbackTreatmentCalculator = new FallbackTreatmentCalculator(new FallbackTreatmentsConfiguration());
            _eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig(), new EventDelivery<SdkEvent, EventMetadata>());

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
            var client = new JSONFileClient($"{rootFilePath}splits_staging_3.json", "", _fallbackTreatmentCalculator, _eventsManager);
            client.BlockUntilReady(1000);

            //Act           
            var result = client.GetTreatment("test", "asd", null);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("off", result);
        }
    }
}
