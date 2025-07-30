using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Classes;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Parsing
{
    public class RuleBasedSegmentParser : Parser, IParser<RuleBasedSegmentDto, RuleBasedSegment>
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(RuleBasedSegmentParser));

        public RuleBasedSegmentParser(ISegmentCacheConsumer segmentCache,
            ISegmentFetcher segmentFetcher) : base(segmentCache, segmentFetcher)
        {
        }

        public RuleBasedSegment Parse(RuleBasedSegmentDto rbsDTO, IRuleBasedSegmentCacheConsumer ruleBasedSegmentCache)
        {
            try
            {
                if (!Enum.TryParse(rbsDTO.Status, out StatusEnum result) || result != StatusEnum.ACTIVE)
                {
                    return null;
                }

                return new RuleBasedSegment
                {
                    Name = rbsDTO.Name,
                    ChangeNumber = rbsDTO.ChangeNumber,
                    Excluded = CheckExcluded(rbsDTO.Excluded),
                    CombiningMatchers = ParseCombiningMatchers(rbsDTO.Conditions, ruleBasedSegmentCache)
                };
            }
            catch (Exception e)
            {
                _log.Error("Exception caught parsing rule-based segment", e);
                return null;
            }
        }

        private List<CombiningMatcher> ParseCombiningMatchers(List<ConditionDefinition> conditions, IRuleBasedSegmentCacheConsumer ruleBasedSegmentCache)
        {
            var toReturn = new List<CombiningMatcher>();

            foreach (ConditionDefinition condition in conditions)
            {
                if (condition.matcherGroup.matchers == null || condition.matcherGroup.matchers.Count == 0)
                {
                    throw new Exception("Missing or empty matchers");
                }

                var delegates = condition
                    .matcherGroup
                    .matchers
                    .Select(m => ParseMatcher(m, ruleBasedSegmentCache))
                    .ToList();

                toReturn.Add(new CombiningMatcher()
                {
                    delegates = delegates,
                    combiner = Helper.ParseCombiner(condition.matcherGroup.combiner)
                });
            }

            return toReturn;
        }

        private static Excluded CheckExcluded(Excluded excluded)
        {
            if (excluded == null)
            {
                return new Excluded();
            }

            if (excluded.Keys == null)
            {
                excluded.Keys = new List<string>();
            }

            if (excluded.Segments == null)
            {
                excluded.Segments = new List<ExcludedSegments>();
            }

            return excluded;
        }
    }
}
