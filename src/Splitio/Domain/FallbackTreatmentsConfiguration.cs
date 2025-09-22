using System.Collections.Generic;

namespace Splitio.Domain
{
    public class FallbackTreatmentsConfiguration
    {
        public FallbackTreatmentsConfiguration() { }

        public FallbackTreatmentsConfiguration(FallbackTreatment globalFallbackTreatment, Dictionary<string, FallbackTreatment> byFlagFallbackTreatment)
        {
            GlobalFallbackTreatment = globalFallbackTreatment;
            ByFlagFallbackTreatment = byFlagFallbackTreatment;
        }

        public FallbackTreatment GlobalFallbackTreatment { get; set; }
        public Dictionary<string, FallbackTreatment> ByFlagFallbackTreatment { get; set; }
    }
}
