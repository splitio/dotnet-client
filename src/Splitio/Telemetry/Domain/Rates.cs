using Newtonsoft.Json;

namespace Splitio.Telemetry.Domain
{
    public class Rates
    {
        [JsonProperty("sp")]
        public long Splits { get; set; }
        [JsonProperty("se")]
        public long Segments { get; set; }
        [JsonProperty("im")]
        public long Impressions { get; set; }
        [JsonProperty("ev")]
        public long Events { get; set; }
        [JsonProperty("te")]
        public long Telemetry { get; set; }
    }
}
