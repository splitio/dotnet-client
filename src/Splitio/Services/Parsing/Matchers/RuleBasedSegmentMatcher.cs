using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Parsing.Matchers
{
    public class RuleBasedSegmentMatcher : BaseMatcher
    {
        private readonly string _segmentName;
        private readonly IRuleBasedSegmentCacheConsumer _ruleBasedSegmentCache;
        private readonly ISegmentCacheConsumer _segmentsCache;

        public RuleBasedSegmentMatcher(string segmentName,
            IRuleBasedSegmentCacheConsumer ruleBasedSegmentCache,
            ISegmentCacheConsumer segmentsCache)
        {
            _segmentName = segmentName;
            _ruleBasedSegmentCache = ruleBasedSegmentCache;
            _segmentsCache = segmentsCache;
        }

        public override bool Match(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return Match(new Key(key, null), attributes, evaluator);
        }

        public override bool Match(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            var rbs = _ruleBasedSegmentCache.Get(_segmentName);
            if (rbs == null)
            {
                return false;
            }

            if (IsExcluded(rbs, key))
            {
                return false;
            }

            if (rbs.CombiningMatchers.Any(cm => cm.Match(key, attributes, evaluator)))
            {
                return true;
            }

            return false;
        }

        public override async Task<bool> MatchAsync(Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            var rbs = await _ruleBasedSegmentCache.GetAsync(_segmentName);
            if (rbs == null)
            {
                return false;
            }

            if (IsExcluded(rbs, key))
            {
                return false;
            }

            foreach (var cm in rbs.CombiningMatchers)
            {
                if (await cm.MatchAsync(key, attributes, evaluator))
                {
                    return true;
                }
            }

            return false;
        }

        public override async Task<bool> MatchAsync(string key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            return await MatchAsync(new Key(key, null), attributes, evaluator);
        }

        private bool IsExcluded(RuleBasedSegment rbs, Key key)
        {
            if (rbs.Excluded.Keys.Contains(key.matchingKey))
            {
                return true;
            }

            if (rbs.Excluded.Segments.Any(s => _segmentsCache.IsInSegment(s, key.matchingKey)))
            {
                return true;
            }

            return false;
        }
    }
}
