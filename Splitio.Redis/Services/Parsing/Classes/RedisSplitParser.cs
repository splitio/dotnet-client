using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Parsing.Classes
{
    public class RedisSplitParser : SplitParser
    {
        public RedisSplitParser(ISegmentCache segmentsCache)
        {
            _segmentsCache = segmentsCache;
        }

        protected override Task<IMatcher> GetInSegmentMatcherAsync(MatcherDefinition matcherDefinition, ParsedSplit parsedSplit)
        {
            var matcherData = matcherDefinition.userDefinedSegmentMatcherData;
            var userDefinedSegmentMatcher = new UserDefinedSegmentMatcher(matcherData.segmentName, _segmentsCache);
            
            return Task.FromResult((IMatcher)userDefinedSegmentMatcher);
        }
    }
}
