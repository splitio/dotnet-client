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
        public long Since { get; set; }
        public long Till { get; set; }
        public List<T> Data { get; set; }
    }
}
