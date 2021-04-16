using Newtonsoft.Json;

namespace Splitio.Telemetry.Domain
{
    public class UrlOverrides
    {
        [JsonProperty("s")]
        public bool Sdk { get; set; }
        [JsonProperty("e")]
        public bool Events { get; set; }
        [JsonProperty("a")]
        public bool Auth { get; set; }
        [JsonProperty("st")]
        public bool Stream { get; set; }
        [JsonProperty("t")]
        public bool Telemetry { get; set; }
    }
}
