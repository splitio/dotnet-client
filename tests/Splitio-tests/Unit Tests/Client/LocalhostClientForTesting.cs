using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Classes;

namespace Splitio_Tests.Unit_Tests.Client
{
    public class LocalhostClientForTesting : LocalhostClient
    {
        public LocalhostClientForTesting(string filePath, FallbackTreatmentCalculator fallbackTreatmentCalculator) : base(new ConfigurationOptions { LocalhostFilePath = filePath }, fallbackTreatmentCalculator)
        { }
    }
}
