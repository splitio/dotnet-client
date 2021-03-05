using Newtonsoft.Json;

namespace Splitio.Telemetry.Domain
{
    public class StreamingEvent
    {
        [JsonProperty("e")]
        public int Type { get; set; }
        [JsonProperty("d")]
        public long Data { get; set; }
        [JsonProperty("t")]
        public long Timestamp { get; set; }
    }
}
