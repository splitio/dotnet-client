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
    public class FeatureFlagSyncHelper : IFeatureFlagSyncHelper
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(FeatureFlagSyncHelper));

        private readonly ISplitParser _featureFlagParser;
        private readonly ISplitCache _featureFlagsCache;
        private readonly IFlagSetsFilter _flagSetsFilter;

        public FeatureFlagSyncHelper(ISplitParser featureFlagParser, ISplitCache featureFlagsCache, IFlagSetsFilter flagSetsFilter)
        {
            _featureFlagParser = featureFlagParser;
            _featureFlagsCache = featureFlagsCache;
            _flagSetsFilter = flagSetsFilter;
        }

        public List<string> UpdateFeatureFlagsFromChanges(List<Split> changes, long till)
        {
            var toAdd = new List<ParsedSplit>();
            var toRemove = new List<ParsedSplit>();
            var segmentNames = new List<string>();

            foreach (var featureFlag in changes)
            {
                var pFeatureFlag = _featureFlagParser.Parse(featureFlag);

                if (pFeatureFlag == null || !_flagSetsFilter.Match(featureFlag.Sets))
                {
                    toRemove.Add(pFeatureFlag);
                    continue;
                }

                toAdd.Add(pFeatureFlag);
                segmentNames.AddRange(featureFlag.GetSegmentNames());
            }

            _featureFlagsCache.Update(toAdd, toRemove, till);

            if (_log.IsDebugEnabled && toAdd.Count > 0)
            {
                _log.Debug($"Added feature flags: {string.Join(" - ", toAdd.Select(s => s.name).ToList())}");
            }

            if (_log.IsDebugEnabled && toRemove.Count > 0)
            {
                _log.Debug($"Deleted feature flags: {string.Join(" - ", toRemove.Select(s => s.name).ToList())}");
            }

            return segmentNames;
        }
    }
}
