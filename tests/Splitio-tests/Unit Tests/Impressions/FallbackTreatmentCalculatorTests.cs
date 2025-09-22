using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Impressions.Classes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class FallbackTreatmentCalculatorTests
    {

        [TestMethod]
        public async Task Works()
        {
            // Arrange.
            FallbackTreatmentsConfiguration fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(new FallbackTreatment("on"), null);
            FallbackTreatmentCalculator fallbackTreatmentCalculator = new FallbackTreatmentCalculator(fallbackTreatmentsConfiguration);
            Assert.AreEqual("on", fallbackTreatmentCalculator.resolve("anyflag", "exception").Treatment);
            Assert.AreEqual("fallback - exception", fallbackTreatmentCalculator.resolve("anyflag", "exception").Label);

            fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(new FallbackTreatment("on"),
                    new Dictionary<string, FallbackTreatment>() {{ "flag", new FallbackTreatment("off") }} );
            fallbackTreatmentCalculator = new FallbackTreatmentCalculator(fallbackTreatmentsConfiguration);
            Assert.AreEqual("on", fallbackTreatmentCalculator.resolve("anyflag", "exception").Treatment);
            Assert.AreEqual("fallback - exception", fallbackTreatmentCalculator.resolve("anyflag", "exception").Label);
            Assert.AreEqual("off", fallbackTreatmentCalculator.resolve("flag", "exception").Treatment);
            Assert.AreEqual("fallback - exception", fallbackTreatmentCalculator.resolve("flag", "exception").Label);

            fallbackTreatmentsConfiguration = new FallbackTreatmentsConfiguration(null,
                new Dictionary<string, FallbackTreatment>() {{ "flag", new FallbackTreatment("off") }
            } );
            fallbackTreatmentCalculator = new FallbackTreatmentCalculator(fallbackTreatmentsConfiguration);
            Assert.AreEqual("control", fallbackTreatmentCalculator.resolve("anyflag", "exception").Treatment);
            Assert.AreEqual("exception", fallbackTreatmentCalculator.resolve("anyflag", "exception").Label);
            Assert.AreEqual("off", fallbackTreatmentCalculator.resolve("flag", "exception").Treatment);
            Assert.AreEqual("fallback - exception", fallbackTreatmentCalculator.resolve("flag", "exception").Label);
        }
    }
}
