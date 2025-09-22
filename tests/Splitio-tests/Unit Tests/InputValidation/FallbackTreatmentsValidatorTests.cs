using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.InputValidation.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.InputValidation
{
    [TestClass]
    public class FallbackTreatmentsValidatorTests
    {

        [TestMethod]
        public void Works()
        {
            IFallbackTreatmentsValidator fallbackTreatmentsValidator = new FallbackTreatmentsValidator();
            FallbackTreatmentsConfiguration fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(new FallbackTreatment("12#2"), null);
                           
            Assert.AreEqual(null, fallbackTreatmentsValidator.validate(fallbackTreatmentsConfiguration, Splitio.Enums.API.GetTreatment).GlobalFallbackTreatment.Treatment);

            fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(null, 
                new Dictionary<string, FallbackTreatment>() { { "flag", new FallbackTreatment("12#2") }} ); 
            Assert.AreEqual(0, fallbackTreatmentsValidator.validate(fallbackTreatmentsConfiguration, Splitio.Enums.API.GetTreatment).ByFlagFallbackTreatment.Count);

            fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(
                        new FallbackTreatment("on"),
                        new Dictionary<string, FallbackTreatment>() { { "flag", new FallbackTreatment("off") } });
            var processed = fallbackTreatmentsValidator.validate(fallbackTreatmentsConfiguration, Splitio.Enums.API.GetTreatment);
            Assert.AreEqual("on", processed.GlobalFallbackTreatment.Treatment);
            processed.ByFlagFallbackTreatment.TryGetValue("flag", out FallbackTreatment fallbackTreatment);
            Assert.AreEqual("off", fallbackTreatment.Treatment);   
        }
    }
}
