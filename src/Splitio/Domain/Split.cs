﻿using System.Collections.Generic;

namespace Splitio.Domain
{
    public class Split : SplitBase
    {        
        public string status { get; set; }
        public List<ConditionDefinition> conditions { get; set; }
        public int? algo { get; set; }
        public int? trafficAllocationSeed { get; set; }

        public List<string> GetSegmentNames()
        {
            var names = new List<string>();

            foreach (var condition in conditions)
            {
                foreach (var matcher in condition.matcherGroup.matchers)
                {
                    if (matcher.userDefinedSegmentMatcherData != null)
                    {
                        names.Add(matcher.userDefinedSegmentMatcherData.segmentName);
                    }
                }
            }

            return names;
        }
    }
}
