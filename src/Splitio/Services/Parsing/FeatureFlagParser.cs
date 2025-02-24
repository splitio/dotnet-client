using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Classes;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Parsing
{
    public class FeatureFlagParser : Parser, IParser<Split, ParsedSplit>
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(FeatureFlagParser));

        public FeatureFlagParser(ISegmentCacheConsumer segmentsCache) : base(segmentsCache)
        {
        }

        public ParsedSplit Parse(Split split)
        {
            try
            {
                var isValidStatus = Enum.TryParse(split.status, out StatusEnum result);

                if (!isValidStatus || result != StatusEnum.ACTIVE)
                {
                    return null;
                }

                var parsedSplit = new ParsedSplit
                {
                    name = split.name,
                    killed = split.killed,
                    defaultTreatment = split.defaultTreatment,
                    seed = split.seed,
                    conditions = new List<ConditionWithLogic>(),
                    changeNumber = split.changeNumber,
                    trafficTypeName = split.trafficTypeName,
                    algo = split.algo == 0 || split.algo == null ? AlgorithmEnum.LegacyHash : (AlgorithmEnum)split.algo,
                    trafficAllocation = split.trafficAllocation,
                    trafficAllocationSeed = split.trafficAllocationSeed ?? 0,
                    configurations = split.configurations,
                    Sets = split.Sets
                };

                return ParseConditions(split.conditions, parsedSplit);
            }
            catch (Exception e)
            {
                _log.Error("Exception caught parsing split", e);
                return null;
            }
        }

        private ParsedSplit ParseConditions(List<ConditionDefinition> conditions, ParsedSplit parsedSplit)
        {
            try
            {
                foreach (var condition in conditions)
                {
                    parsedSplit.conditions.Add(new ConditionWithLogic()
                    {
                        conditionType = Enum.TryParse(condition.conditionType, out ConditionType result) ? result : ConditionType.WHITELIST,
                        partitions = condition.partitions,
                        matcher = ParseMatcherGroup(parsedSplit, condition.matcherGroup),
                        label = condition.label
                    });
                }
            }
            catch (UnsupportedMatcherException ex)
            {
                _log.Error(ex.Message);

                parsedSplit.conditions = Helper.GetDefaultConditions();
            }

            return parsedSplit;
        }

        private CombiningMatcher ParseMatcherGroup(ParsedSplit parsedSplit, MatcherGroupDefinition matcherGroupDefinition)
        {
            if (matcherGroupDefinition.matchers == null || matcherGroupDefinition.matchers.Count == 0)
            {
                throw new Exception("Missing or empty matchers");
            }

            var delegates = matcherGroupDefinition
                .matchers
                .Select(ParseMatcher)
                .ToList();

            return new CombiningMatcher()
            {
                delegates = delegates,
                combiner = Helper.ParseCombiner(matcherGroupDefinition.combiner)
            };
        }
    }
}
