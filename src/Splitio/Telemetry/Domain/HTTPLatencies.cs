using Newtonsoft.Json;
using System.Collections.Generic;

namespace Splitio.Telemetry.Domain
{
    public class HTTPLatencies
    {
        [JsonProperty("sp")]
        public IList<long> Splits { get; set; }
        [JsonProperty("se")]
        public IList<long> Segments { get; set; }
        [JsonProperty("im")]
        public IList<long> Impressions { get; set; }
        [JsonProperty("ev")]
        public IList<long> Events { get; set; }
        [JsonProperty("to")]
        public IList<long> Token { get; set; }
        [JsonProperty("te")]
        public IList<long> Telemetry { get; set; }
    }
}
