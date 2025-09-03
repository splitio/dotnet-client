using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Storages;
using Splitio_Tests.Resources;
using System;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class ImpressionsSdkApiClientTests
    {
        private readonly IWrapperAdapter _wrapperAdapter = WrapperAdapter.Instance();

        [TestMethod]
        public void CorrectFormatSendCounts()
        {
            // Arrange.
            var time9am = SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 00, 00, DateTimeKind.Utc));
            var impressions = new List<ImpressionsCountModel> { new ImpressionsCountModel(new KeyCache("featur1", time9am), 2) };
            
            // Act.
            var result = ImpressionsSdkApiClient.ConvertToJson(impressions);

            // Assert.
            var expected = $"{{\"pf\":[{{\"f\":\"featur1\",\"m\":{time9am},\"rc\":2}}]}}";
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CorrectFormatSendImpressions()
        {
            // Arrange.
            var impressions = new List<KeyImpression>
            {
                new KeyImpression("matching-key", "feature-1", "treatment", 34534546, 3333444, "label", "bucketing-key", false),
                new KeyImpression("matching-key", "feature-1", "treatment", 34534550, 3333444, "label", "bucketing-key", false, 34534546),
                new KeyImpression("matching-key", "feature-2", "treatment", 34534546, 3333444, "label", "bucketing-key", false),
            };
            impressions[2].Properties = "{\"prop\":\"val\"}";
            // Act.
            var result = ImpressionsSdkApiClient.ConvertToJson(impressions);

            // Assert.
            var expected = "[{\"f\":\"feature-1\",\"i\":[{\"k\":\"matching-key\",\"t\":\"treatment\",\"m\":34534546,\"c\":3333444,\"r\":\"label\",\"b\":\"bucketing-key\"},{\"k\":\"matching-key\",\"t\":\"treatment\",\"m\":34534550,\"c\":3333444,\"r\":\"label\",\"b\":\"bucketing-key\"}]},{\"f\":\"feature-2\",\"i\":[{\"k\":\"matching-key\",\"t\":\"treatment\",\"m\":34534546,\"c\":3333444,\"r\":\"label\",\"b\":\"bucketing-key\",\"properties\":\"{\\\"prop\\\":\\\"val\\\"}\"}]}]";
            Assert.AreEqual(expected, result);
        }
    }
}
