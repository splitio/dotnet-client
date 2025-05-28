using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Filters;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Shared.Classes
{
    public class FeatureFlagUpdater : IUpdater<Split>
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(FeatureFlagUpdater));

        private readonly IParser<Split, ParsedSplit> _featureFlagParser;
        private readonly IFeatureFlagCacheProducer _featureFlagsCache;
        private readonly IFlagSetsFilter _flagSetsFilter;
        private readonly IRuleBasedSegmentCacheConsumer _ruleBasedSegmentCache;

        public FeatureFlagUpdater(IParser<Split, ParsedSplit> featureFlagParser,
            IFeatureFlagCacheProducer featureFlagsCache,
            IFlagSetsFilter flagSetsFilter,
            IRuleBasedSegmentCacheConsumer ruleBasedSegmentCache)
        {
            _featureFlagParser = featureFlagParser;
            _featureFlagsCache = featureFlagsCache;
            _flagSetsFilter = flagSetsFilter;
            _ruleBasedSegmentCache = ruleBasedSegmentCache;
        }

        public Dictionary<Enums.SegmentType, List<string>> Process(List<Split> changes, long till)
        {
            var toAdd = new List<ParsedSplit>();
            var toRemove = new List<string>();
            var toReturn = new Dictionary<Enums.SegmentType, List<string>>
            {
                { Enums.SegmentType.Standard, new List<string>() },
                { Enums.SegmentType.RuleBased, new List<string>() }
            };

            foreach (var featureFlag in changes)
            {
                var ffParsed = _featureFlagParser.Parse(featureFlag, _ruleBasedSegmentCache);

                if (ffParsed == null || !_flagSetsFilter.Intersect(featureFlag.Sets))
                {
                    toRemove.Add(featureFlag.name);
                    continue;
                }

                toAdd.Add(ffParsed);
                toReturn[Enums.SegmentType.Standard].AddRange(ffParsed.GetSegments());
                toReturn[Enums.SegmentType.RuleBased].AddRange(ffParsed.GetRuleBasedSegments());
            }

            _featureFlagsCache.Update(toAdd, toRemove, till);

            if (_log.IsDebugEnabled && toAdd.Count > 0)
            {
                _log.Debug($"Added feature flags: {string.Join(" - ", toAdd.Select(s => s.name).ToList())}");
            }

            if (_log.IsDebugEnabled && toRemove.Count > 0)
            {
                _log.Debug($"Deleted feature flags: {string.Join(" - ", toRemove)}");
            }

            return toReturn;
        }
    }
}
