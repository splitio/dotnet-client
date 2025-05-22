using Splitio.Services.Parsing;
using Splitio.Services.Parsing.Matchers;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Domain
{
    public class ParsedSplit : SplitBase
    {
        public ParsedSplit()
        {
            conditions = new List<ConditionWithLogic>();
            Prerequisites = new PrerequisitesMatcher();
        }

        public List<ConditionWithLogic> conditions { get; set; }
        public AlgorithmEnum algo { get; set; }
        public int trafficAllocationSeed { get; set; }
        public PrerequisitesMatcher Prerequisites { get; set; }

        public SplitView ToSplitView()
        {
            var condition = conditions
                .Where(x => x.conditionType == ConditionType.ROLLOUT)
                .FirstOrDefault() ?? conditions.FirstOrDefault();

            var treatments = condition != null ? condition.partitions.Select(y => y.treatment).ToList() : new List<string>();

            return new SplitView
            {
                name = name,
                killed = killed,
                changeNumber = changeNumber,
                treatments = treatments,
                trafficType = trafficTypeName,
                configs = configurations,
                defaultTreatment = defaultTreatment,
                sets = Sets != null ? Sets.ToList() : new List<string>(),
                impressionsDisabled = ImpressionsDisabled
            };
        }

        public List<string> GetRuleBasedSegments()
        {
            var toReturn = new List<string>();

            foreach (var condition in conditions)
            {
                foreach (var del in condition.matcher.delegates)
                {
                    if (del.matcher is RuleBasedSegmentMatcher matcher)
                    {
                        toReturn.Add(matcher.GetRuleBasedSegmentName());
                    }
                }
            }

            return toReturn;
        }

        public List<string> GetSegments()
        {
            var segments = new List<string>();

            foreach (var condition in conditions)
            {
                foreach (var del in condition.matcher.delegates)
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
}
