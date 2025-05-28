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

            if (rbs.Excluded.Keys.Contains(key.matchingKey))
            {
                return false;
            }

            if (MatchExcludedSegments(rbs.Excluded.Segments, key, attributes, evaluator))
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

            if (rbs.Excluded.Keys.Contains(key.matchingKey))
            {
                return false;
            }

            if (await MatchExcludedSegmentsAsync(rbs.Excluded.Segments, key, attributes, evaluator))
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

        public string GetRuleBasedSegmentName()
        {
            return _segmentName;
        }

        private async Task<bool> MatchExcludedSegmentsAsync(List<ExcludedSegments> excludedSegments, Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            foreach (var excludedSegment in excludedSegments)
            {
                if (excludedSegment.IsStandard && await _segmentsCache.IsInSegmentAsync(excludedSegment.Name, key.matchingKey))
                {
                    return true;
                }

                if (excludedSegment.IsRuleBased)
                {
                    var rbsMatcher = new RuleBasedSegmentMatcher(excludedSegment.Name, _ruleBasedSegmentCache, _segmentsCache);
                    if (await rbsMatcher.MatchAsync(key, attributes, evaluator))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool MatchExcludedSegments(List<ExcludedSegments> excludedSegments, Key key, Dictionary<string, object> attributes = null, IEvaluator evaluator = null)
        {
            foreach (var excludedSegment in excludedSegments)
            {
                if (excludedSegment.IsStandard && _segmentsCache.IsInSegment(excludedSegment.Name, key.matchingKey))
                {
                    return true;
                }

                if (excludedSegment.IsRuleBased)
                {
                    var rbsMatcher = new RuleBasedSegmentMatcher(excludedSegment.Name, _ruleBasedSegmentCache, _segmentsCache);
                    if (rbsMatcher.Match(key, attributes, evaluator))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
