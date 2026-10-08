using Newtonsoft.Json;

namespace Splitio.Commons.Telemetry.Domain
{
    public class UpdatesFromSSE
    {
        [JsonProperty("sp")]
        public long Splits { get; set; }
    }
}
