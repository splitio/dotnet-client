using System.Collections.Generic;

namespace Splitio.Domain
{
    public class RuleBasedSegment
    {
        public string Name { get; set; }
        public long ChangeNumber { get; set; }
        public List<CombiningMatcher> CombiningMatchers { get; set; }
    }
}
