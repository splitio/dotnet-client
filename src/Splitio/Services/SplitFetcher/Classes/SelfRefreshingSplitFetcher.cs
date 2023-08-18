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
        private readonly ISplitCache _splitCache;
        private readonly ISplitTask _periodicTask;
        private readonly IFeatureFlagSyncHelper _helper;

        public SelfRefreshingSplitFetcher(ISplitChangeFetcher splitChangeFetcher,
            IStatusManager statusManager,
            ISplitCache splitCache,
            ISplitTask periodicTask,
            IFeatureFlagSyncHelper helper)
        {
            _splitChangeFetcher = splitChangeFetcher;
            _statusManager = statusManager;
            _splitCache = splitCache;
            _helper = helper;
            _periodicTask = periodicTask;
            _periodicTask.SetAction(async () => await FetchSplits(new FetchOptions()));
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

        public async Task ClearAsync()
        {
            _splitCache.Clear();
            await _periodicTask.StopAsync();
            _log.Debug("FeatureFlags cache disposed ...");
        }

        public async Task<FetchResult> FetchSplits(FetchOptions fetchOptions)
        {
            var segmentNames = new List<string>();
            var success = false;

            while (!_statusManager.IsDestroyed())
            {   
                var changeNumber = _splitCache.GetChangeNumber();

                try
                {
                    var result = await _splitChangeFetcher.Fetch(changeNumber, fetchOptions);

                    if (result == null)
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
                        var sNames = _helper.UpdateFeatureFlagsFromChanges(result.splits, result.till);
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
                        _log.Debug($"split fetch before: {changeNumber}, after: {_splitCache.GetChangeNumber()}");
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
