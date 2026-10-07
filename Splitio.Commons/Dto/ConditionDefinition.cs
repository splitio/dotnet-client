using System.Collections.Generic;

namespace Splitio.Commons.Dto
{
    public class ConditionDefinition
    {
        public string conditionType { get; set; }
        public MatcherGroupDefinition matcherGroup { get; set; }
        public List<PartitionDefinition> partitions { get; set; }
        public string label { get; set; }
    }
}
