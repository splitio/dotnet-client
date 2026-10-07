using Newtonsoft.Json;
using System.Collections.Generic;

namespace Splitio.Commons.Dto
{
    public class PrerequisitesDto
    {
        [JsonProperty("n")]
        public string FeatureFlagName { get; set; }
        [JsonProperty("ts")]
        public List<string> Treatments { get; set; }
    }
}
