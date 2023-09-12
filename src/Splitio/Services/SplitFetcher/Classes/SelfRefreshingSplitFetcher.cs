using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Classes
{
    public class SelfRefreshingSplitFetcher : ISplitFetcher
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SelfRefreshingSplitFetcher));

        private readonly ISplitChangeFetcher _splitChangeFetcher;
        private readonly ISplitParser _splitParser;
        private readonly IStatusManager _statusManager;
        private readonly ISplitTask _periodicTask;
        private readonly IFeatureFlagCache _featureFlagCache;

        public SelfRefreshingSplitFetcher(ISplitChangeFetcher splitChangeFetcher,
            ISplitParser splitParser,
            IStatusManager statusManager,
            ISplitTask periodicTask,
            IFeatureFlagCache featureFlagCache)
        {
            _splitChangeFetcher = splitChangeFetcher;
            _splitParser = splitParser;
            _statusManager = statusManager;
            _featureFlagCache = featureFlagCache;
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
                        segmentNames.AddRange(UpdateSplitsFromChangeFetcherResponse(result.splits));
                        _featureFlagCache.SetChangeNumber(result.till);
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

        #region Private Methods
        private IList<string> UpdateSplitsFromChangeFetcherResponse(List<Split> splitChanges)
        {
            var addedSplits = new List<Split>();
            var removedSplits = new List<Split>();
            var segmentNames = new List<string>();

            foreach (Split split in splitChanges)
            {
                //If not active --> Remove Split
                var isValidStatus = Enum.TryParse(split.status, out StatusEnum result);

                if (!isValidStatus || result != StatusEnum.ACTIVE)
                {
                    _featureFlagCache.RemoveSplit(split.name);
                    removedSplits.Add(split);
                }
                else
                {
                    var isUpdated = _featureFlagCache.AddOrUpdate(split.name, _splitParser.Parse(split));

                    if (!isUpdated)
                    {
                        //If not existing in _splits, its a new split
                        addedSplits.Add(split);
                    }

                    segmentNames.AddRange(split.GetSegments());
                }
            }

            if (_log.IsDebugEnabled && addedSplits.Count() > 0)
            {
                var addedFeatureNames = addedSplits
                    .Select(x => x.name)
                    .ToList();

                _log.Debug(string.Format("Added features: {0}", string.Join(" - ", addedFeatureNames)));
            }

            if (_log.IsDebugEnabled && removedSplits.Count() > 0)
            {
                var removedFeatureNames = removedSplits
                    .Select(x => x.name)
                    .ToList();

                _log.Debug(string.Format("Deleted features: {0}", string.Join(" - ", removedFeatureNames)));
            }

            return segmentNames;
        }
        #endregion
    }
}
