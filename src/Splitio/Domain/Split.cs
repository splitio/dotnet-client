using System.Collections.Generic;

namespace Splitio.Domain
{
    public class Split : SplitBase
    {        
        public string Status { get; set; }
        public List<Condition> Conditions { get; set; }
        public int? Algo { get; set; }
        public int? TrafficAllocationSeed { get; set; }

        public List<string> GetSegments()
        {
            var segments = new List<string>();

            foreach (var condition in Conditions)
            {
                foreach (var matcher in condition.MatcherGroup.Matchers)
                {
                    if (matcher.UserDefinedSegmentMatcherData != null)
                    {
                        segments.Add(matcher.UserDefinedSegmentMatcherData.segmentName);
                    }
                }
            }

            return segments;
        }
    }
}
