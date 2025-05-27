using Newtonsoft.Json;
using System.Collections.Generic;

namespace Splitio.Domain
{
    public class PrerequisitesDto
    {
        [JsonProperty("n")]
        public string FeatureFlagName { get; set; }
        [JsonProperty("ts")]
        public List<string> Treatments { get; set; }
    }
}
