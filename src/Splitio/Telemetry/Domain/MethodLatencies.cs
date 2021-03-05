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
        public IList<long> TreatmenstWithConfig { get; set; }
        [JsonProperty("tr")]
        public IList<long> Track { get; set; }
    }
}
