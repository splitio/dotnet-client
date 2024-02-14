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
    public abstract class SplitParser : ISplitParser
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitParser));

        protected ISegmentCacheConsumer _segmentsCache;

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

                return ParseConditions(split, parsedSplit);
            }
            catch (Exception e)
            {
                _log.Error("Exception caught parsing split", e);
                return null;
            }
        }

        protected abstract IMatcher GetInSegmentMatcher(MatcherDefinition matcherDefinition, ParsedSplit parsedSplit);

        private ParsedSplit ParseConditions(Split split, ParsedSplit parsedSplit)
        {
            foreach (var condition in split.conditions)
            {
                var isValidCondition = Enum.TryParse(condition.conditionType, out ConditionType result);

                parsedSplit.conditions.Add(new ConditionWithLogic()
                {
                    conditionType = isValidCondition ? result : ConditionType.WHITELIST,
                    partitions = condition.partitions,
                    matcher = ParseMatcherGroup(parsedSplit, condition.matcherGroup),
                    label = condition.label
                });
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
                combiner = ParseCombiner(matcherGroupDefinition.combiner)
            };
        }

        private AttributeMatcher ParseMatcher(ParsedSplit parsedSplit, MatcherDefinition matcherDefinition)
        {
            if (matcherDefinition.matcherType == null)
            {
                throw new Exception("Missing matcher type value");
            }

            var matcherType = matcherDefinition.matcherType;

            IMatcher matcher = null;
            try
            {
                var isValidMatcherType = Enum.TryParse(matcherType, out MatcherTypeEnum result);

                if (isValidMatcherType)
                {
                    switch (result)
                    {
                        case MatcherTypeEnum.ALL_KEYS:
                            matcher = new AllKeysMatcher(); break;
                        case MatcherTypeEnum.BETWEEN:
                            matcher = new BetweenMatcher(matcherDefinition.betweenMatcherData); break;
                        case MatcherTypeEnum.EQUAL_TO:
                            matcher = new EqualToMatcher(matcherDefinition.unaryNumericMatcherData); break;
                        case MatcherTypeEnum.GREATER_THAN_OR_EQUAL_TO:
                            matcher = new GreaterOrEqualToMatcher(matcherDefinition.unaryNumericMatcherData); break;
                        case MatcherTypeEnum.IN_SEGMENT:
                            matcher = GetInSegmentMatcher(matcherDefinition, parsedSplit); break;
                        case MatcherTypeEnum.LESS_THAN_OR_EQUAL_TO:
                            matcher = new LessOrEqualToMatcher(matcherDefinition.unaryNumericMatcherData); break;
                        case MatcherTypeEnum.WHITELIST:
                            matcher = new WhitelistMatcher(matcherDefinition.whitelistMatcherData); break;
                        case MatcherTypeEnum.EQUAL_TO_SET:
                            matcher = new EqualToSetMatcher(matcherDefinition.whitelistMatcherData); break;
                        case MatcherTypeEnum.CONTAINS_ANY_OF_SET:
                            matcher = new ContainsAnyOfSetMatcher(matcherDefinition.whitelistMatcherData); break;
                        case MatcherTypeEnum.CONTAINS_ALL_OF_SET:
                            matcher = new ContainsAllOfSetMatcher(matcherDefinition.whitelistMatcherData); break;
                        case MatcherTypeEnum.PART_OF_SET:
                            matcher = new PartOfSetMatcher(matcherDefinition.whitelistMatcherData); break;
                        case MatcherTypeEnum.STARTS_WITH:
                            matcher = new StartsWithMatcher(matcherDefinition.whitelistMatcherData); break;
                        case MatcherTypeEnum.ENDS_WITH:
                            matcher = new EndsWithMatcher(matcherDefinition.whitelistMatcherData); break;
                        case MatcherTypeEnum.CONTAINS_STRING:
                            matcher = new ContainsStringMatcher(matcherDefinition.whitelistMatcherData); break;
                        case MatcherTypeEnum.IN_SPLIT_TREATMENT:
                            matcher = new DependencyMatcher(matcherDefinition.dependencyMatcherData); break;
                        case MatcherTypeEnum.EQUAL_TO_BOOLEAN:
                            matcher = new EqualToBooleanMatcher(matcherDefinition.booleanMatcherData); break;
                        case MatcherTypeEnum.MATCHES_STRING:
                            matcher = new MatchesStringMatcher(matcherDefinition.stringMatcherData); break;
                        case MatcherTypeEnum.SEMVER:
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
                throw new Exception($"Unable to create matcher for matcher type: {matcherType}");
            }

            var attributeMatcher = new AttributeMatcher()
            {
                matcher = matcher,
                negate = matcherDefinition.negate
            };

            if (matcherDefinition.keySelector != null && matcherDefinition.keySelector.attribute != null)
            {
                attributeMatcher.attribute = matcherDefinition.keySelector.attribute;
            }

            return attributeMatcher;
        }

        private static CombinerEnum ParseCombiner(string combinerEnum)
        {
            _ = Enum.TryParse(combinerEnum, out CombinerEnum result);

            return result;
        }
    }
}
