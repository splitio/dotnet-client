using System.Collections.Generic;

namespace Splitio.Domain
{
    public class RuleBasedSegment
    {
        public string Name { get; set; }
        public long ChangeNumber { get; set; }
        public Excluded Excluded { get; set; }
        public List<CombiningMatcher> CombiningMatchers { get; set; }
    }

    public class Excluded
    {
        public List<string> Keys { get; set; }
        public List<string> Segments { get; set; }
    }
}
