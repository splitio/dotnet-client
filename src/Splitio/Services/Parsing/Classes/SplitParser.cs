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
    public class SplitParser : ISplitParser
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitParser));
        private readonly ISegmentCacheConsumer _segmentsCache;

        public SplitParser(ISegmentCacheConsumer segmentsCache)
        {
            _segmentsCache = segmentsCache;
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

        protected virtual IMatcher GetInSegmentMatcher(MatcherDefinition matcherDefinition, ParsedSplit parsedSplit)
        {
            return new UserDefinedSegmentMatcher(matcherDefinition.userDefinedSegmentMatcherData.segmentName, _segmentsCache);
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

            return new CombiningMatcher()
            {
                delegates = matcherGroupDefinition.matchers.Select(x => ParseMatcher(parsedSplit, x)).ToList(),
                combiner = Helper.ParseCombiner(matcherGroupDefinition.combiner)
            };
        }

        private AttributeMatcher ParseMatcher(ParsedSplit parsedSplit, MatcherDefinition mDefinition)
        {
            if (mDefinition.matcherType == null)
            {
                throw new Exception("Missing matcher type value");
            }

            IMatcher matcher = null;
            try
            {
                if (Enum.TryParse(mDefinition.matcherType, out MatcherTypeEnum result))
                {
                    switch (result)
                    {
                        case MatcherTypeEnum.ALL_KEYS:
                            matcher = new AllKeysMatcher();
                            break;
                        case MatcherTypeEnum.BETWEEN:
                            var betweenData = mDefinition.betweenMatcherData;
                            matcher = new BetweenMatcher(betweenData.dataType, betweenData.start, betweenData.end);
                            break;
                        case MatcherTypeEnum.EQUAL_TO:
                            var equalToData = mDefinition.unaryNumericMatcherData;
                            matcher = new EqualToMatcher(equalToData.dataType, equalToData.value);
                            break;
                        case MatcherTypeEnum.GREATER_THAN_OR_EQUAL_TO:
                            var gtoetData = mDefinition.unaryNumericMatcherData;
                            matcher = new GreaterOrEqualToMatcher(gtoetData.dataType, gtoetData.value);
                            break;
                        case MatcherTypeEnum.IN_SEGMENT:
                            matcher = GetInSegmentMatcher(mDefinition, parsedSplit);
                            break;
                        case MatcherTypeEnum.LESS_THAN_OR_EQUAL_TO:
                            var ltoetData = mDefinition.unaryNumericMatcherData;
                            matcher = new LessOrEqualToMatcher(ltoetData.dataType, ltoetData.value);
                            break;
                        case MatcherTypeEnum.WHITELIST:
                            matcher = new WhitelistMatcher(mDefinition.whitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.EQUAL_TO_SET:
                            matcher = new EqualToSetMatcher(mDefinition.whitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.CONTAINS_ANY_OF_SET:
                            matcher = new ContainsAnyOfSetMatcher(mDefinition.whitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.CONTAINS_ALL_OF_SET:
                            matcher = new ContainsAllOfSetMatcher(mDefinition.whitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.PART_OF_SET:
                            matcher = new PartOfSetMatcher(mDefinition.whitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.STARTS_WITH:
                            matcher = new StartsWithMatcher(mDefinition.whitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.ENDS_WITH:
                            matcher = new EndsWithMatcher(mDefinition.whitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.CONTAINS_STRING:
                            matcher = new ContainsStringMatcher(mDefinition.whitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.IN_SPLIT_TREATMENT:
                            var dependecyData = mDefinition.dependencyMatcherData;
                            matcher = new DependencyMatcher(dependecyData.split, dependecyData.treatments);
                            break;
                        case MatcherTypeEnum.EQUAL_TO_BOOLEAN:
                            matcher = new EqualToBooleanMatcher(mDefinition.booleanMatcherData.Value);
                            break;
                        case MatcherTypeEnum.MATCHES_STRING:
                            matcher = new MatchesStringMatcher(mDefinition.stringMatcherData);
                            break;
                        case MatcherTypeEnum.EQUAL_TO_SEMVER:
                            matcher = new EqualToSemverMatcher(mDefinition.stringMatcherData);
                            break;
                        case MatcherTypeEnum.GREATER_THAN_OR_EQUAL_TO_SEMVER:
                            matcher = new GreaterThanOrEqualToSemverMatcher(mDefinition.stringMatcherData);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error parsing matcher", ex);
            }

            if (matcher == null)
            {
                throw new UnsupportedMatcherException($"Unable to create matcher for matcher type: {mDefinition.matcherType}");
            }

            var attributeMatcher = new AttributeMatcher()
            {
                matcher = matcher,
                negate = mDefinition.negate
            };

            if (mDefinition.keySelector != null && mDefinition.keySelector.attribute != null)
            {
                attributeMatcher.attribute = mDefinition.keySelector.attribute;
            }

            return attributeMatcher;
        }
    }
}
