using Newtonsoft.Json;

namespace Splitio.Telemetry.Domain
{
    public class UpdatesFromSSE
    {
        [JsonProperty("sp")]
        public long Splits { get; set; }
    }
}
