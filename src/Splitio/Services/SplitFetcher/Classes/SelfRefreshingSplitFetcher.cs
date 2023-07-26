using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Classes
{
    public class SelfRefreshingSplitFetcher : ISplitFetcher
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SelfRefreshingSplitFetcher));

        private readonly ISplitChangeFetcher _splitChangeFetcher;
        private readonly ISplitParser _splitParser;
        private readonly IStatusManager _statusManager;
        private readonly ISplitCache _splitCache;        
        private readonly int _interval;
        private readonly ITasksManager _taskManager;

        private readonly object _lock = new object();
        private CancellationTokenSource _cancelTokenSource;
        private bool _running;

        public SelfRefreshingSplitFetcher(ISplitChangeFetcher splitChangeFetcher,
            ISplitParser splitParser,
            IStatusManager statusManager,
            int interval,
            ITasksManager taskManager,
            ISplitCache splitCache = null)
        {
            _splitChangeFetcher = splitChangeFetcher;
            _splitParser = splitParser;
            _statusManager = statusManager;
            _interval = interval;
            _splitCache = splitCache;
            _taskManager = taskManager;
        }

        #region Public Methods
        public void Start()
        {
            lock (_lock)
            {
                if (_running || _statusManager.IsDestroyed()) return;

                _running = true;
                _cancelTokenSource = new CancellationTokenSource();

                _taskManager.StartPeriodic(async () =>
                {
                    await FetchSplits(new FetchOptions());
                }, _interval * 1000, _cancelTokenSource, "Splits Fetcher.");
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_running) return;

                _running = false;
                _cancelTokenSource.Cancel();
                _cancelTokenSource.Dispose();
            }
        }

        public void Clear()
        {
            _splitCache.Clear();
        }

        public async Task<FetchResult> FetchSplits(FetchOptions fetchOptions)
        {
            var segmentNames = new List<string>();
            var success = false;

            while (true)
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
                        segmentNames.AddRange(UpdateSplitsFromChangeFetcherResponse(result.splits));
                        _splitCache.SetChangeNumber(result.till);
                    }
                }
                catch (Exception e)
                {
                    _log.Error("Exception caught refreshing splits", e);
                    Stop();
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
                    _splitCache.RemoveSplit(split.name);
                    removedSplits.Add(split);
                }
                else
                {
                    var isUpdated = _splitCache.AddOrUpdate(split.name, _splitParser.Parse(split));

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
