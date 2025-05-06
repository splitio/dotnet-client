using Newtonsoft.Json;
using System.Collections.Generic;

namespace Splitio.Domain
{
    public class OldSplitChangesDto
    {
        public long Since { get; set; }
        public long Till { get; set; }
        public List<Split> Splits { get; set; }

        public TargetingRulesDto ToTargetingRulesDto(bool clearCache = false)
        {
            return new TargetingRulesDto
            {
                FeatureFlags = new ChangesDto<Split>
                {
                    Data = Splits,
                    Since = Since,
                    Till = Till
                },
                RuleBasedSegments = new ChangesDto<RuleBasedSegmentDto>
                {
                    Data = new List<RuleBasedSegmentDto>(),
                    Since = -1,
                    Till = -1
                },
                ClearCache = clearCache
            };
        }
    }

    public class TargetingRulesDto
    {
        [JsonProperty("ff")]
        public ChangesDto<Split> FeatureFlags { get; set; }
        [JsonProperty("rbs")]
        public ChangesDto<RuleBasedSegmentDto> RuleBasedSegments { get; set; }
        [JsonIgnore]
        public bool ClearCache { get; set; }
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
