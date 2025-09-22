using System.Collections.Generic;

namespace Splitio.Domain
{
    public class FallbackTreatmentsConfiguration
    {
        public FallbackTreatmentsConfiguration() 
        {
            GlobalFallbackTreatment = null;
            ByFlagFallbackTreatment = null;
        }

        public FallbackTreatmentsConfiguration(FallbackTreatment globalFallbackTreatment)
        {
            GlobalFallbackTreatment = globalFallbackTreatment;
            ByFlagFallbackTreatment = null;
        }

        public FallbackTreatmentsConfiguration(Dictionary<string, FallbackTreatment> byFlagFallbackTreatment)
        {
            GlobalFallbackTreatment = null;
            ByFlagFallbackTreatment = byFlagFallbackTreatment;
        }

        public FallbackTreatmentsConfiguration(FallbackTreatment globalFallbackTreatment, Dictionary<string, FallbackTreatment> byFlagFallbackTreatment)
        {
            GlobalFallbackTreatment = globalFallbackTreatment;
            ByFlagFallbackTreatment = byFlagFallbackTreatment;
        }

        public FallbackTreatmentsConfiguration(FallbackTreatment globalFallbackTreatment, Dictionary<string, string> byFlagFallbackTreatment)
        {
            GlobalFallbackTreatment = globalFallbackTreatment;
            ByFlagFallbackTreatment = buildByFlag(byFlagFallbackTreatment); 
        }

        public FallbackTreatmentsConfiguration(string globalFallbackTreatment)
        {
            GlobalFallbackTreatment = new FallbackTreatment(globalFallbackTreatment);
            ByFlagFallbackTreatment = null;
        }

        public FallbackTreatmentsConfiguration(string globalFallbackTreatment, Dictionary<string, FallbackTreatment> byFlagFallbackTreatment)
        {
            GlobalFallbackTreatment = new FallbackTreatment(globalFallbackTreatment);
            ByFlagFallbackTreatment = byFlagFallbackTreatment;
        }

        public FallbackTreatmentsConfiguration(Dictionary<string, string> byFlagFallbackTreatment)
        {
            GlobalFallbackTreatment = null;
            ByFlagFallbackTreatment = buildByFlag(byFlagFallbackTreatment);
        }

        public FallbackTreatmentsConfiguration(string globalFallbackTreatment, Dictionary<string, string> byFlagFallbackTreatment)
        {
            GlobalFallbackTreatment = new FallbackTreatment(globalFallbackTreatment);
            ByFlagFallbackTreatment = buildByFlag(byFlagFallbackTreatment);
        }

        public FallbackTreatment GlobalFallbackTreatment { get; set; }
        public Dictionary<string, FallbackTreatment> ByFlagFallbackTreatment { get; set; }

        private Dictionary<string, FallbackTreatment> buildByFlag(Dictionary<string, string> byFlagString)
        {
            var byFlagFallbackTreatment = new Dictionary<string, FallbackTreatment>();
            foreach (var byflag in byFlagString)
            {
                byFlagFallbackTreatment.Add(byflag.Key, new FallbackTreatment(byflag.Value));
            }
            return byFlagFallbackTreatment;

        }
    }
}
