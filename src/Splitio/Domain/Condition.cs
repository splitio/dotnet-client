using System.Collections.Generic;

namespace Splitio.Domain
{
    public class Condition
    {
        public string ConditionType { get; set; }
        public MatcherGroup MatcherGroup { get; set; }
        public List<Partition> Partitions { get; set; }
        public string Label { get; set; }
    }
}
