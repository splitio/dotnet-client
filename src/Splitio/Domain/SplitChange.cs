using Newtonsoft.Json;
using System.Collections.Generic;

namespace Splitio.Domain
{
    public class SplitChangesResult
    {
        [JsonProperty("ff")]
        public ChangesDto<Split> FeatureFlags { get; set; }
        [JsonProperty("rbs")]
        public ChangesDto<RuleBasedSegmentDto> RuleBasedSegments { get; set; }
    }

    public class ChangesDto<T> where T : class
    {
        [JsonProperty("s")]
        public long Since { get; set; }
        [JsonProperty("t")]
        public long Till { get; set; }
        [JsonProperty("d")]
        public List<T> Data { get; set; }
    }
}
