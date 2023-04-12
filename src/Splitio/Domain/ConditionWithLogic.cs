using System.Collections.Generic;

namespace Splitio.Domain
{
    public class ConditionWithLogic
    {
        public ConditionType ConditionType { get; set; }
        public CombiningMatcher Matcher { get; set; }
        public List<PartitionDefinition> Partitions { get; set; }
        public string Label { get; set; }
    }
}
