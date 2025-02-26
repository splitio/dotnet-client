using Splitio.Services.Parsing;
using System.Collections.Generic;

namespace Splitio.Domain
{
    public class RuleBasedSegment
    {
        public string Name { get; set; }
        public long ChangeNumber { get; set; }
        public Excluded Excluded { get; set; }
        public List<CombiningMatcher> CombiningMatchers { get; set; }

        public List<string> GetSegments()
        {
            var segments = new List<string>();

            foreach (var cm in CombiningMatchers)
            {
                foreach (var del in cm.delegates)
                {
                    if (del.matcher is UserDefinedSegmentMatcher matcher)
                    {
                        segments.Add(matcher.GetSegmentName());
                    }
                }
            }

            return segments;
        }
    }

    public class Excluded
    {
        public List<string> Keys { get; set; }
        public List<string> Segments { get; set; }
    }
}
