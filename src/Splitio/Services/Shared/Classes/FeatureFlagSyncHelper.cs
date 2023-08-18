using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
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
        private readonly HashSet<string> _flagSets;
        private readonly bool _filterFlagSets;

        public FeatureFlagSyncHelper(ISplitParser featureFlagParser, ISplitCache featureFlagsCache, HashSet<string> flagSets)
        {
            _featureFlagParser = featureFlagParser;
            _featureFlagsCache = featureFlagsCache;
            _flagSets = flagSets;
            _filterFlagSets = _flagSets.Any();
        }

        public List<string> UpdateFeatureFlagsFromChanges(List<Split> changes, long till)
        {
            var added = new List<string>();
            var removed = new List<string>();
            var segmentNames = new List<string>();

            foreach (var featureFlag in changes)
            {
                var pFeatureFlag = _featureFlagParser.Parse(featureFlag);

                if (pFeatureFlag == null || !FlagSetsMatch(featureFlag.Sets))
                {
                    _featureFlagsCache.RemoveSplit(featureFlag.name);
                    removed.Add(featureFlag.name);
                    continue;
                }

                segmentNames.AddRange(featureFlag.GetSegmentNames());

                if (_featureFlagsCache.AddOrUpdate(featureFlag.name, pFeatureFlag))
                {
                    added.Add(featureFlag.name);
                }
            }

            _featureFlagsCache.SetChangeNumber(till);

            if (_log.IsDebugEnabled && added.Count > 0)
            {
                _log.Debug($"Added feature flags: {string.Join(" - ", added)}");
            }

            if (_log.IsDebugEnabled && removed.Count > 0)
            {
                _log.Debug($"Deleted feature flags: {string.Join(" - ", removed)}");
            }

            return segmentNames;
        }

        private bool FlagSetsMatch(HashSet<string> sets)
        {
            if (!_filterFlagSets) return true;

            if (sets == null) return false;

            return _flagSets.Intersect(sets).Any();
        }
    }
}
