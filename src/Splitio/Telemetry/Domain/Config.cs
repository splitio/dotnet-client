using Newtonsoft.Json;
using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Telemetry.Domain
{
    public class Config
    {
        [JsonProperty("oM")]
        public int OperationMode { get; set; }
        [JsonProperty("sE")]
        public bool StreamingEnabled { get; set; }
        [JsonProperty("st")]
        public string Storage { get; set; }
        [JsonProperty("rR")]
        public Rates Rates { get; set; }
        [JsonProperty("uO")]
        public UrlOverrides UrlOverrides { get; set; }
        [JsonProperty("iQ")]
        public long ImpressionsQueueSize { get; set; }
        [JsonProperty("eQ")]
        public long EventsQueueSize { get; set; }
        [JsonProperty("iM")]
        public ImpressionsMode ImpressionsMode { get; set; }
        [JsonProperty("iL")]
        public bool ImpressionListenerEnabled { get; set; }
        [JsonProperty("hp")]
        public bool HTTPProxyDetected { get; set; }
        [JsonProperty("aF")]
        public long ActiveFactories { get; set; }
        [JsonProperty("rF")]
        public long RedundantActiveFactories { get; set; }
        [JsonProperty("tR")]
        public long TimeUntilSDKReady { get; set; }
        [JsonProperty("bT")]
        public long BURTimeouts { get; set; }
        [JsonProperty("nR")]
        public long SDKNotReadyUsage { get; set; }
        [JsonProperty("t")]
        public List<string> Tags { get; set; }
        [JsonProperty("i")]
        public List<string> Integrations { get; set; }
        [JsonProperty("fsT")]
        public int FlagSetsTotal { get; set; }
        [JsonProperty("fsI")]
        public int FlagSetsInvalid { get; set; }
    }
}
