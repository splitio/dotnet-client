using System.Collections.Generic;

namespace Splitio.Domain
{
    public class RuleBasedSegmentDTO
    {
        public string Name { get; set; }
        public long ChangeNumber { get; set; }
        public string Status { get; set; }
        public List<ConditionDefinition> Conditions { get; set; }
    }
}
