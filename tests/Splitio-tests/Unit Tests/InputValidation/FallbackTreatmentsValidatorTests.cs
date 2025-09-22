using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.InputValidation.Classes;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.InputValidation
{
    [TestClass]
    public class FallbackTreatmentsValidatorTests
    {

        [TestMethod]
        public void Works()
        {
            FallbackTreatmentsValidator fallbackTreatmentsValidator = new FallbackTreatmentsValidator();

            FallbackTreatmentsConfiguration fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(new FallbackTreatment("12#2"));
            Assert.AreEqual(null, fallbackTreatmentsValidator.validate(fallbackTreatmentsConfiguration, Splitio.Enums.API.GetTreatment).GlobalFallbackTreatment.Treatment);

            fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration("12#2");
            Assert.AreEqual(null, fallbackTreatmentsValidator.validate(fallbackTreatmentsConfiguration, Splitio.Enums.API.GetTreatment).GlobalFallbackTreatment.Treatment);

            fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(
                new Dictionary<string, FallbackTreatment>() { { "flag", new FallbackTreatment("12#2") } });
            Assert.AreEqual(0, fallbackTreatmentsValidator.validate(fallbackTreatmentsConfiguration, Splitio.Enums.API.GetTreatment).ByFlagFallbackTreatment.Count);

            fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(
                new Dictionary<string, string>() { { "flag", "12#2" } });
            Assert.AreEqual(0, fallbackTreatmentsValidator.validate(fallbackTreatmentsConfiguration, Splitio.Enums.API.GetTreatment).ByFlagFallbackTreatment.Count);

            fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(
                        new FallbackTreatment("on"),
                        new Dictionary<string, FallbackTreatment>() { { "flag", new FallbackTreatment("off") } });
            var processed = fallbackTreatmentsValidator.validate(fallbackTreatmentsConfiguration, Splitio.Enums.API.GetTreatment);
            Assert.AreEqual("on", processed.GlobalFallbackTreatment.Treatment);
            processed.ByFlagFallbackTreatment.TryGetValue("flag", out FallbackTreatment fallbackTreatment);
            Assert.AreEqual("off", fallbackTreatment.Treatment);

            fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration("on",
                        new Dictionary<string, FallbackTreatment>() { { "flag", new FallbackTreatment("off") } });
            processed = fallbackTreatmentsValidator.validate(fallbackTreatmentsConfiguration, Splitio.Enums.API.GetTreatment);
            Assert.AreEqual("on", processed.GlobalFallbackTreatment.Treatment);
            processed.ByFlagFallbackTreatment.TryGetValue("flag", out FallbackTreatment fallbackTreatment2);
            Assert.AreEqual("off", fallbackTreatment2.Treatment);

            fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(
                        new FallbackTreatment("on"),
                        new Dictionary<string, string>() { { "flag", "off" } });
            processed = fallbackTreatmentsValidator.validate(fallbackTreatmentsConfiguration, Splitio.Enums.API.GetTreatment);
            Assert.AreEqual("on", processed.GlobalFallbackTreatment.Treatment);
            processed.ByFlagFallbackTreatment.TryGetValue("flag", out fallbackTreatment);
            Assert.AreEqual("off", fallbackTreatment.Treatment);

            fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(
                        new FallbackTreatment("on"),
                        new Dictionary<string, string>() { { "flag", "off" } });
            processed = fallbackTreatmentsValidator.validate(fallbackTreatmentsConfiguration, Splitio.Enums.API.GetTreatment);
            Assert.AreEqual("on", processed.GlobalFallbackTreatment.Treatment);
            processed.ByFlagFallbackTreatment.TryGetValue("flag", out  fallbackTreatment);
            Assert.AreEqual("off", fallbackTreatment.Treatment);
        }
    }
}
