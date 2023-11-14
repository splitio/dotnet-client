using Newtonsoft.Json;

namespace Splitio.Telemetry.Domain
{
    public class MethodExceptions
    {
        [JsonProperty("t")]
        public long Treatment { get; set; }
        [JsonProperty("ts")]
        public long Treatments { get; set; }
        [JsonProperty("tc")]
        public long TreatmentWithConfig { get; set; }
        [JsonProperty("tcs")]
        public long TreatmentsWithConfig { get; set; }
        [JsonProperty("tf")]
        public long TreatmentsByFlagSet { get; set; }
        [JsonProperty("tfs")]
        public long TreatmentsByFlagSets { get; set; }
        [JsonProperty("tcf")]
        public long TreatmentsWithConfigByFlagSet { get; set; }
        [JsonProperty("tcfs")]
        public long TreatmentsWithConfigByFlagSets { get; set; }
        [JsonProperty("tr")]
        public long Track { get; set; }
    }
}
