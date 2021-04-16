using Newtonsoft.Json;
using System.Collections.Generic;

namespace Splitio.Telemetry.Domain
{
    public class Stats
    {
        [JsonProperty("lS")]
        public LastSynchronization LastSynchronizations { get; set; }
        [JsonProperty("mL")]
        public MethodLatencies MethodLatencies { get; set; }
        [JsonProperty("mE")]
        public MethodExceptions MethodExceptions { get; set; }
        [JsonProperty("hE")]
        public HTTPErrors HTTPErrors { get; set; }
        [JsonProperty("hL")]
        public HTTPLatencies HTTPLatencies { get; set; }
        [JsonProperty("tR")]
        public long TokenRefreshes { get; set; }
        [JsonProperty("aR")]
        public long AuthRejections { get; set; }
        [JsonProperty("iQ")]
        public long ImpressionsQueued { get; set; }
        [JsonProperty("iDe")]
        public long ImpressionsDeduped { get; set; }
        [JsonProperty("iDr")]
        public long ImpressionsDropped { get; set; }
        [JsonProperty("spC")]
        public long SplitCount { get; set; }
        [JsonProperty("seC")]
        public long SegmentCount { get; set; }
        [JsonProperty("skC")]
        public long SegmentKeyCount { get; set; }
        [JsonProperty("sL")]
        public long SessionLengthMs { get; set; }
        [JsonProperty("eQ")]
        public long EventsQueued { get; set; }
        [JsonProperty("eD")]
        public long EventsDropped { get; set; }
        [JsonProperty("sE")]
        public List<StreamingEvent> StreamingEvents { get; set; }
        [JsonProperty("t")]
        public List<string> Tags { get; set; }
    }
}
