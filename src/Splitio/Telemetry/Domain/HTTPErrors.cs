using Newtonsoft.Json;
using System.Collections.Generic;

namespace Splitio.Telemetry.Domain
{
    public class HTTPErrors
    {
        [JsonProperty("sp")]
        public IDictionary<int, long> Splits { get; set; }
        [JsonProperty("se")]
        public IDictionary<int, long> Segments { get; set; }
        [JsonProperty("im")]
        public IDictionary<int, long> Impressions { get; set; }
        [JsonProperty("ic")]
        public IDictionary<int, long> ImpressionCount { get; set; }
        [JsonProperty("ev")]
        public IDictionary<int, long> Events { get; set; }
        [JsonProperty("to")]
        public IDictionary<int, long> Token { get; set; }
        [JsonProperty("te")]
        public IDictionary<int, long> Telemetry { get; set; }
    }
}
