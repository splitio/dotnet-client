using System.Collections.Generic;

namespace Splitio.Domain
{
    public class RuleBasedSegmentDto
    {
        public string Name { get; set; }
        public long ChangeNumber { get; set; }
        public string Status { get; set; }
        public Excluded Excluded { get; set; }
        public List<ConditionDefinition> Conditions { get; set; }
    }

    public class Excluded
    {
        public List<string> Keys { get; set; }
        public List<string> Segments { get; set; }
    }
}
