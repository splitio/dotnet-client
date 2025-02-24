using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.SegmentFetcher.Interfaces;

namespace Splitio.Services.Parsing.Classes
{
    public class InMemorySplitParser : FeatureFlagParser
    {
        private readonly ISegmentFetcher _segmentFetcher;

        public InMemorySplitParser(ISegmentFetcher segmentFetcher, ISegmentCacheConsumer segmentsCache) : base(segmentsCache)
        {
            _segmentFetcher = segmentFetcher;
        }

        protected override IMatcher GetInSegmentMatcher(MatcherDefinition matcherDefinition)
        {
            _segmentFetcher.InitializeSegment(matcherDefinition.userDefinedSegmentMatcherData.segmentName);

            return base.GetInSegmentMatcher(matcherDefinition);
        }
    }
}
