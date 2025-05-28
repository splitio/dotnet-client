using Splitio.Services.Parsing;
using System.Collections.Generic;
using System.Linq;

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
            var segments = new HashSet<string>();

            foreach (var segment in Excluded.Segments)
            {
                if (segment.IsStandard)
                {
                    segments.Add(segment.Name);
                }
            }

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

            return segments.ToList();
        }
    }

    public class Excluded
    {
        public List<string> Keys { get; set; }
        public List<ExcludedSegments> Segments { get; set; }

        public Excluded()
        {
            Keys = new List<string>();
            Segments = new List<ExcludedSegments>();
        }
    }

    public class ExcludedSegments
    {
        public string Type { get; set; }
        public string Name { get; set; }

        public bool IsStandard => Type.Equals("standard");
        public bool IsRuleBased => Type.Equals("rule-based");
    }
}
