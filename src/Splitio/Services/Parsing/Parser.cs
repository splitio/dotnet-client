using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Classes;
using Splitio.Services.SegmentFetcher.Interfaces;
using Splitio.Services.Shared.Classes;
using System;

namespace Splitio.Services.Parsing
{
    public class Parser
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(Parser));

        private readonly ISegmentCacheConsumer _segmentsCache;
        private readonly ISegmentFetcher _segmentFetcher;

        public Parser(ISegmentCacheConsumer segmentCache,
            ISegmentFetcher segmentFetcher = null)
        {
            _segmentsCache = segmentCache;
            _segmentFetcher = segmentFetcher;
        }

        public AttributeMatcher ParseMatcher(MatcherDefinition mDefinition)
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
                            matcher = GetInSegmentMatcher(mDefinition);
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
                        case MatcherTypeEnum.LESS_THAN_OR_EQUAL_TO_SEMVER:
                            matcher = new LessThanOrEqualToSemverMatcher(mDefinition.stringMatcherData);
                            break;
                        case MatcherTypeEnum.BETWEEN_SEMVER:
                            matcher = new BetweenSemverMatcher(mDefinition.BetweenStringMatcherData.start, mDefinition.BetweenStringMatcherData.end);
                            break;
                        case MatcherTypeEnum.IN_LIST_SEMVER:
                            matcher = new InListSemverMatcher(mDefinition.whitelistMatcherData.whitelist);
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

        private IMatcher GetInSegmentMatcher(MatcherDefinition matcherDefinition)
        {
            if (_segmentFetcher != null)
            {
                _segmentFetcher.InitializeSegment(matcherDefinition.userDefinedSegmentMatcherData.segmentName);
            }

            return new UserDefinedSegmentMatcher(matcherDefinition.userDefinedSegmentMatcherData.segmentName, _segmentsCache);
        }
    }
}