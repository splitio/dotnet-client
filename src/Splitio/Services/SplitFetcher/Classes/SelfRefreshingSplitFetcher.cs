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
    public class SelfRefreshingSplitFetcher : ISplitFetcher
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SelfRefreshingSplitFetcher));

        private readonly ISplitChangeFetcher _splitChangeFetcher;
        private readonly IStatusManager _statusManager;
        private readonly ISplitTask _periodicTask;
        private readonly IFeatureFlagCache _featureFlagCache;
        private readonly IFeatureFlagSyncService _featureFlagSyncService;

        public SelfRefreshingSplitFetcher(ISplitChangeFetcher splitChangeFetcher,
            IStatusManager statusManager,
            ISplitTask periodicTask,
            IFeatureFlagCache featureFlagCache,
            IFeatureFlagSyncService featureFlagSyncService)
        {
            _splitChangeFetcher = splitChangeFetcher;
            _statusManager = statusManager;
            _featureFlagCache = featureFlagCache;
            _featureFlagSyncService = featureFlagSyncService;
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
            _log.Debug("FeatureFlags cache disposed ...");
        }

        public async Task<FetchResult> FetchSplitsAsync(FetchOptions fetchOptions)
        {
            var segmentNames = new List<string>();
            var success = false;

            while (!_statusManager.IsDestroyed())
            {   
                var changeNumber = _featureFlagCache.GetChangeNumber();

                try
                {
                    var result = await _splitChangeFetcher.FetchAsync(changeNumber, fetchOptions);

                    if (result == null || _statusManager.IsDestroyed())
                    {
                        break;
                    }

                    if (changeNumber >= result.till)
                    {
                        success = true;
                        //There are no new split changes
                        break;
                    }

                    if (result.splits != null && result.splits.Count > 0)
                    {
                        var sNames = _featureFlagSyncService.UpdateFeatureFlagsFromChanges(result.splits, result.till);
                        segmentNames.AddRange(sNames);
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
                        _log.Debug($"split fetch before: {changeNumber}, after: {_featureFlagCache.GetChangeNumber()}");
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
