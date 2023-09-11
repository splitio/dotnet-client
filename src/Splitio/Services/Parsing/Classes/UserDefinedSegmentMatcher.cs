using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;

namespace Splitio.Services.Parsing
{
    public class UserDefinedSegmentMatcher : BaseMatcher
    {
        private readonly string _segmentName;
        private readonly ISegmentCacheConsumer _segmentsCache;

        public UserDefinedSegmentMatcher(string segmentName, ISegmentCacheConsumer segmentsCache)
        {
            _segmentName = segmentName;
            _segmentsCache = segmentsCache;
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return _segmentsCache.IsInSegment(_segmentName, key);
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Match(key.matchingKey, attributes, evaluator);
        }
    }
}
