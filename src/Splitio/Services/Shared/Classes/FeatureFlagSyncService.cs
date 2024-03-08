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
    public class FeatureFlagSyncService : IFeatureFlagSyncService
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(FeatureFlagSyncService));

        private readonly ISplitParser _featureFlagParser;
        private readonly IFeatureFlagCacheProducer _featureFlagsCache;
        private readonly IFlagSetsFilter _flagSetsFilter;

        public FeatureFlagSyncService(ISplitParser featureFlagParser,
            IFeatureFlagCacheProducer featureFlagsCache,
            IFlagSetsFilter flagSetsFilter)
        {
            _featureFlagParser = featureFlagParser;
            _featureFlagsCache = featureFlagsCache;
            _flagSetsFilter = flagSetsFilter;
        }

        public List<string> UpdateFeatureFlagsFromChanges(List<Split> changes, long till)
        {
            var toAdd = new List<ParsedSplit>();
            var toRemove = new List<string>();
            var segmentNames = new List<string>();

            foreach (var featureFlag in changes)
            {
                var ffParsed = _featureFlagParser.Parse(featureFlag);

                if (ffParsed == null || !_flagSetsFilter.Intersect(featureFlag.Sets))
                {
                    toRemove.Add(featureFlag.Name);
                    continue;
                }

                toAdd.Add(ffParsed);
                segmentNames.AddRange(featureFlag.GetSegments());
            }

            _featureFlagsCache.Update(toAdd, toRemove, till);

            if (_log.IsDebugEnabled && toAdd.Count > 0)
            {
                _log.Debug($"Added feature flags: {string.Join(" - ", toAdd.Select(s => s.Name).ToList())}");
            }

            if (_log.IsDebugEnabled && toRemove.Count > 0)
            {
                _log.Debug($"Deleted feature flags: {string.Join(" - ", toRemove)}");
            }

            return segmentNames;
        }
    }
}
