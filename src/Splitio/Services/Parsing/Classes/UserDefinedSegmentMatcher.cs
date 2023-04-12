using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing
{
    public class UserDefinedSegmentMatcher : BaseMatcher
    {
        private readonly string _segmentName;
        private readonly ISegmentCache _segmentsCache;

        public UserDefinedSegmentMatcher(string segmentName, ISegmentCache segmentsCache)
        {
            _segmentName = segmentName;
            _segmentsCache = segmentsCache;
        }

        public override async Task<bool> Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return await _segmentsCache.IsInSegmentAsync(_segmentName, key);
        }

        public override Task<bool> Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Match(key.matchingKey, attributes, evaluator);
        }

        public override bool Match(DateTime key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public override bool Match(long key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public override bool Match(List<string> key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }

        public override bool Match(bool key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return false;
        }
    }
}
