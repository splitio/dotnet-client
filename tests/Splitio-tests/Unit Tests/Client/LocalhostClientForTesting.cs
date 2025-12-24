using Splitio.Domain;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
using Splitio.Services.Impressions.Classes;

namespace Splitio_Tests.Unit_Tests.Client
{
    public class LocalhostClientForTesting : LocalhostClient
    {
        public LocalhostClientForTesting(string filePath, FallbackTreatmentCalculator fallbackTreatmentCalculator) : base(new ConfigurationOptions { LocalhostFilePath = filePath }, fallbackTreatmentCalculator, new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig(), new EventDelivery<SdkEvent, EventMetadata>()))
        { }
    }
}
