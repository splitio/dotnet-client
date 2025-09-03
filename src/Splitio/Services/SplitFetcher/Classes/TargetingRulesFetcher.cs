using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Classes
{
    public class TargetingRulesFetcher : ITargetingRulesFetcher
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(TargetingRulesFetcher));

        private readonly ISplitChangeFetcher _splitChangeFetcher;
        private readonly IStatusManager _statusManager;
        private readonly ISplitTask _periodicTask;
        private readonly IFeatureFlagCache _featureFlagCache;
        private readonly IUpdater<Split> _featureFlagUpdater;
        private readonly IUpdater<RuleBasedSegmentDto> _ruleBasedSegmentUpdater;
        private readonly IRuleBasedSegmentCache _ruleBasedSegmentCache;

        public TargetingRulesFetcher(ISplitChangeFetcher splitChangeFetcher,
            IStatusManager statusManager,
            ISplitTask periodicTask,
            IFeatureFlagCache featureFlagCache,
            IUpdater<Split> featureFlagUpdater,
            IUpdater<RuleBasedSegmentDto> ruleBasedSegmentUpdater,
            IRuleBasedSegmentCache ruleBasedSegmentCache)
        {
            _splitChangeFetcher = splitChangeFetcher;
            _statusManager = statusManager;
            _featureFlagCache = featureFlagCache;
            _featureFlagUpdater = featureFlagUpdater;
            _ruleBasedSegmentUpdater = ruleBasedSegmentUpdater;
            _ruleBasedSegmentCache = ruleBasedSegmentCache;
            _periodicTask = periodicTask;
            _periodicTask.SetFunction(async () => await FetchSplitsAsync(new FetchOptions()));
        }

        #region Public Methods
        public void Start()
        {
            _periodicTask.Start();
        }

        public async Task StopAsync()
        {
            await _periodicTask.StopAsync();
        }

        public void Clear()
        {
            _featureFlagCache.Clear();
            _ruleBasedSegmentCache.Clear();
            _log.Debug("FeatureFlags cache disposed ...");
        }

        public async Task<FetchResult> FetchSplitsAsync(FetchOptions fetchOptions)
        {
            var segmentNames = new List<string>();
            var success = false;

            while (!_statusManager.IsDestroyed())
            {   
                var ffChangeNumber = _featureFlagCache.GetChangeNumber();
                var rbsChangeNumber = _ruleBasedSegmentCache.GetChangeNumber();

                fetchOptions.FeatureFlagsSince = ffChangeNumber;
                fetchOptions.RuleBasedSegmentsSince = rbsChangeNumber;

                try
                {
                    var result = await _splitChangeFetcher.FetchAsync(fetchOptions);

                    if (result == null || _statusManager.IsDestroyed())
                    {
                        break;
                    }

                    if (ffChangeNumber >= result.FeatureFlags.Till && rbsChangeNumber >= result.RuleBasedSegments.Till)
                    {
                        success = true;
                        //There are no new split changes
                        break;
                    }

                    if (result.ClearCache)
                    {
                        _log.Warn($"Forcing a cache cleanup because a different Spec Version was detected.");

                        _featureFlagCache.Clear();
                        _ruleBasedSegmentCache.Clear();
                    }

                    if (result.RuleBasedSegments.Data != null && result.RuleBasedSegments.Data.Count > 0)
                    {
                        var segments = _ruleBasedSegmentUpdater.Process(result.RuleBasedSegments.Data, result.RuleBasedSegments.Till);
                        segmentNames.AddRange(segments[Enums.SegmentType.Standard]);
                    }

                    if (result.FeatureFlags.Data != null && result.FeatureFlags.Data.Count > 0)
                    {
                        var segments = _featureFlagUpdater.Process(result.FeatureFlags.Data, result.FeatureFlags.Till);
                        segmentNames.AddRange(segments[Enums.SegmentType.Standard]);
                    }
                }
                catch (Exception e)
                {
                    _log.Error("Exception caught refreshing splits", e);
                    await StopAsync();
                }
                finally
                {
                    if (_log.IsDebugEnabled)
                    {
                        _log.Debug($"split fetch before: {ffChangeNumber} & {rbsChangeNumber}, after: {_featureFlagCache.GetChangeNumber()} & {_ruleBasedSegmentCache.GetChangeNumber()}");
                    }
                }
            }

            return new FetchResult
            {
                Success = success,
                SegmentNames = segmentNames
            };
        }
        #endregion
    }
}
