using Newtonsoft.Json;
using System.Collections.Generic;

namespace Splitio.Telemetry.Domain
{
    public class MethodLatencies
    {
        [JsonProperty("t")]
        public IList<long> Treatment { get; set; }
        [JsonProperty("ts")]
        public IList<long> Treatments { get; set; }
        [JsonProperty("tc")]
        public IList<long> TreatmentWithConfig { get; set; }
        [JsonProperty("tcs")]
        public IList<long> TreatmentsWithConfig { get; set; }
        [JsonProperty("tf")]
        public IList<long> TreatmentsByFlagSet { get; set; }
        [JsonProperty("tfs")]
        public IList<long> TreatmentsByFlagSets { get; set; }
        [JsonProperty("tcf")]
        public IList<long> TreatmentsWithConfigByFlagSet { get; set; }
        [JsonProperty("tcfs")]
        public IList<long> TreatmentsWithConfigByFlagSets { get; set; }
        [JsonProperty("tr")]
        public IList<long> Track { get; set; }
    }
}
