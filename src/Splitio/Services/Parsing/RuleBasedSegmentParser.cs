using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Classes;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.SegmentFetcher.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Parsing
{
    public class RuleBasedSegmentParser : Parser, IParser<RuleBasedSegmentDto, RuleBasedSegment>
    {
        public RuleBasedSegmentParser(ISegmentCacheConsumer segmentCache,
            ISegmentFetcher segmentFetcher) : base(segmentCache, segmentFetcher)
        {
        }

        public RuleBasedSegment Parse(RuleBasedSegmentDto rbsDTO)
        {
            if (!Enum.TryParse(rbsDTO.Status, out StatusEnum result) || result != StatusEnum.ACTIVE)
            {
                return null;
            }

            return new RuleBasedSegment
            {
                Name = rbsDTO.Name,
                ChangeNumber = rbsDTO.ChangeNumber,
                Excluded = rbsDTO.Excluded,
                CombiningMatchers = ParseCombiningMatchers(rbsDTO.Conditions)
            };
        }

        private List<CombiningMatcher> ParseCombiningMatchers(List<ConditionDefinition> conditions)
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
                    .Select(ParseMatcher)
                    .ToList();

                toReturn.Add(new CombiningMatcher()
                {
                    delegates = delegates,
                    combiner = Helper.ParseCombiner(condition.matcherGroup.combiner)
                });
            }

            return toReturn;
        }
    }
}
