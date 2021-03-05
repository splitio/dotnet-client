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
        public long TreatmenstWithConfig { get; set; }
        [JsonProperty("tr")]
        public long Track { get; set; }
    }
}
