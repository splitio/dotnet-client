using Newtonsoft.Json;
using System.Collections.Generic;

namespace Splitio.Telemetry.Domain
{
    public class HTTPErrors
    {
        [JsonProperty("sp")]
        public IDictionary<long, long> Splits { get; set; }
        [JsonProperty("se")]
        public IDictionary<long, long> Segments { get; set; }
        [JsonProperty("im")]
        public IDictionary<long, long> Impressions { get; set; }
        [JsonProperty("ev")]
        public IDictionary<long, long> Events { get; set; }
        [JsonProperty("to")]
        public IDictionary<long, long> Token { get; set; }
        [JsonProperty("te")]
        public IDictionary<long, long> Telemetry { get; set; }
    }
}
