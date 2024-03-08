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
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitParser));

        private readonly ISegmentCacheConsumer _segmentsCache;

        protected abstract IMatcher GetInSegmentMatcher(MatcherDefinition matcherDefinition, ParsedSplit parsedSplit);

        public ParsedSplit Parse(Split split)
        {
            try
            {
                var isValidStatus = Enum.TryParse(split.Status, out StatusEnum result);

                if (!isValidStatus || result != StatusEnum.ACTIVE)
                {
                    return null;
                }

                var parsedSplit = new ParsedSplit
                {
                    Name = split.Name,
                    Killed = split.Killed,
                    DefaultTreatment = split.DefaultTreatment,
                    Seed = split.Seed,
                    Conditions = new List<ConditionWithLogic>(),
                    ChangeNumber = split.ChangeNumber,
                    TrafficTypeName = split.TrafficTypeName,
                    Algo = split.Algo == 0 || split.Algo == null ? AlgorithmEnum.LegacyHash : (AlgorithmEnum)split.Algo,
                    TrafficAllocation = split.TrafficAllocation,
                    TrafficAllocationSeed = split.TrafficAllocationSeed ?? 0,
                    Configurations = split.Configurations,
                    Sets = split.Sets
                };

                return ParseConditions(split.Conditions, parsedSplit);
            }
            catch (Exception e)
            {
                _log.Error("Exception caught parsing split", e);
                return null;
            }
        }

        private ParsedSplit ParseConditions(List<ConditionDefinition> conditions, ParsedSplit parsedSplit)
        {
            foreach (var condition in conditions)
            {
                try
                {
                    parsedSplit.Conditions.Add(new ConditionWithLogic()
                    {
                        ConditionType = Enum.TryParse(condition.ConditionType, out ConditionType result) ? result : ConditionType.WHITELIST,
                        Partitions = condition.Partitions,
                        Matcher = ParseMatcherGroup(parsedSplit, condition.MatcherGroup),
                        Label = condition.Label
                    });
                }
                catch (UnsupportedMatcherException ex)
                {
                    _log.Error(ex.Message);

                    parsedSplit.conditions = GetDefaultConditions();
                }
            }

            return parsedSplit;
        }

        private CombiningMatcher ParseMatcherGroup(ParsedSplit parsedSplit, MatcherGroup matcherGroupDefinition)
        {
            if (matcherGroupDefinition.Matchers == null || matcherGroupDefinition.Matchers.Count == 0)
            {
                throw new Exception("Missing or empty matchers");
            }

            return new CombiningMatcher()
            {
                delegates = matcherGroupDefinition.matchers.Select(x => ParseMatcher(parsedSplit, x)).ToList(),
                combiner = ParseCombiner(matcherGroupDefinition.combiner)
            };
        }

        private AttributeMatcher ParseMatcher(ParsedSplit parsedSplit, Matcher mGroup)
        {
            if (mGroup.MatcherType == null)
            {
                throw new Exception("Missing matcher type value");
            }

            IMatcher matcher = null;
            try
            {
                if (Enum.TryParse(mGroup.MatcherType, out MatcherTypeEnum result))
                {
                    switch (result)
                    {
                        case MatcherTypeEnum.ALL_KEYS:
                            matcher = new AllKeysMatcher();
                            break;
                        case MatcherTypeEnum.BETWEEN:
                            var betweenData = mGroup.BetweenMatcherData;
                            matcher = new BetweenMatcher(betweenData.dataType, betweenData.start, betweenData.end);
                            break;
                        case MatcherTypeEnum.EQUAL_TO:
                            var equalToData = mGroup.UnaryNumericMatcherData;
                            matcher = new EqualToMatcher(equalToData.dataType, equalToData.value);
                            break;
                        case MatcherTypeEnum.GREATER_THAN_OR_EQUAL_TO:
                            var gtoetData = mGroup.UnaryNumericMatcherData;
                            matcher = new GreaterOrEqualToMatcher(gtoetData.dataType, gtoetData.value);
                            break;
                        case MatcherTypeEnum.IN_SEGMENT:
                            matcher = GetInSegmentMatcher(mGroup, parsedSplit);
                            break;
                        case MatcherTypeEnum.LESS_THAN_OR_EQUAL_TO:
                            var ltoetData = mGroup.UnaryNumericMatcherData;
                            matcher = new LessOrEqualToMatcher(ltoetData.dataType, ltoetData.value);
                            break;
                        case MatcherTypeEnum.WHITELIST:
                            matcher = new WhitelistMatcher(mGroup.WhitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.EQUAL_TO_SET:
                            matcher = new EqualToSetMatcher(mGroup.WhitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.CONTAINS_ANY_OF_SET:
                            matcher = new ContainsAnyOfSetMatcher(mGroup.WhitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.CONTAINS_ALL_OF_SET:
                            matcher = new ContainsAllOfSetMatcher(mGroup.WhitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.PART_OF_SET:
                            matcher = new PartOfSetMatcher(mGroup.WhitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.STARTS_WITH:
                            matcher = new StartsWithMatcher(mGroup.WhitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.ENDS_WITH:
                            matcher = new EndsWithMatcher(mGroup.WhitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.CONTAINS_STRING:
                            matcher = new ContainsStringMatcher(mGroup.WhitelistMatcherData.whitelist);
                            break;
                        case MatcherTypeEnum.IN_SPLIT_TREATMENT:
                            var dependecyData = mGroup.DependencyMatcherData;
                            matcher = new DependencyMatcher(dependecyData.split, dependecyData.treatments);
                            break;
                        case MatcherTypeEnum.EQUAL_TO_BOOLEAN:
                            matcher = new EqualToBooleanMatcher(mGroup.BooleanMatcherData.Value);
                            break;
                        case MatcherTypeEnum.MATCHES_STRING:
                            matcher = new MatchesStringMatcher(mGroup.StringMatcherData);
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
                throw new UnsupportedMatcherException($"Unable to create matcher for matcher type: {mGroup.MatcherType}");
            }

            var attributeMatcher = new AttributeMatcher()
            {
                matcher = matcher,
                negate = mGroup.Negate
            };

            if (mGroup.KeySelector != null && mGroup.KeySelector.attribute != null)
            {
                attributeMatcher.attribute = mGroup.KeySelector.attribute;
            }

            return attributeMatcher;
        }

        private static CombinerEnum ParseCombiner(string combinerEnum)
        {
            _ = Enum.TryParse(combinerEnum, out CombinerEnum result);

            return result;
        }

        private static List<ConditionWithLogic> GetDefaultConditions()
        {
            return new List<ConditionWithLogic>
            {
                new ConditionWithLogic()
                {
                    conditionType = ConditionType.WHITELIST,
                    label = "unsupported matcher type",
                    partitions = new List<PartitionDefinition>
                    {
                        new PartitionDefinition
                        {
                            size = 100,
                            treatment = "control"
                        }
                    },
                    matcher = new CombiningMatcher
                    {
                        combiner = CombinerEnum.AND,
                        delegates = new List<AttributeMatcher>
                        {
                            new AttributeMatcher
                            {
                                matcher = new AllKeysMatcher(),
                            }
                        }
                    }
                }
            };
        }
    }
}
