using Splitio.CommonLibraries;
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
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(SelfRefreshingSplitFetcher));

        private readonly ISplitChangeFetcher _splitChangeFetcher;
        private readonly ISplitParser _splitParser;
        private readonly IReadinessGatesCache _gates;
        private readonly ISplitCache _splitCache;        
        private readonly int _interval;
        private readonly ITasksManager _taskManager;

        private readonly object _lock = new object();
        private CancellationTokenSource _cancelTokenSource;
        private bool _running;

        public SelfRefreshingSplitFetcher(ISplitChangeFetcher splitChangeFetcher,
            ISplitParser splitParser,
            IReadinessGatesCache gates,
            int interval,
            ITasksManager taskManager,
            ISplitCache splitCache = null)
        {
            _splitChangeFetcher = splitChangeFetcher;
            _splitParser = splitParser;
            _gates = gates;
            _interval = interval;
            _splitCache = splitCache;
            _taskManager = taskManager;
        }

        #region Public Methods
        public void Start()
        {
            _taskManager.Start(() =>
            {
                lock (_lock)
                {
                    if (_running) return;

                    _running = true;
                    _cancelTokenSource = new CancellationTokenSource();
                    _gates.WaitUntilSdkInternalReady();

                    _taskManager.StartPeriodic(() =>
                    {
                        FetchSplits(new FetchOptions()).Wait();
                    }, _interval * 1000, _cancelTokenSource, "Splits Fetcher.");
                }
            }, new CancellationTokenSource(), "Main Splits Fetcher.");
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

        public async Task<IList<string>> FetchSplits(FetchOptions fetchOptions)
        {
            var segmentNames = new List<string>();
            var names = new Dictionary<string, string>();

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
                        _gates.SplitsAreReady();
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

            return segmentNames;
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

                    segmentNames.AddRange(GetSegmentNames(split));
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

        public IList<string> GetSegmentNames(Split split)
        {
            var names = new List<string>();

            foreach (var condition in split.conditions)
            {
                foreach (var matcher in condition.matcherGroup.matchers)
                {
                    if (matcher.userDefinedSegmentMatcherData != null)
                    {
                        names.Add(matcher.userDefinedSegmentMatcherData.segmentName);
                    }
                }
            }

            return names;
        }
        #endregion
    }
}
