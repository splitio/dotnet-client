using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.InputValidation.Interfaces;
using Splitio.Services.Parsing.Classes;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Client.Classes
{
    public class JSONFileClient : SplitClient
    {
        private readonly IFeatureFlagCache _featureFlagCache;
        private readonly ISegmentCache _segmentCache;

        public JSONFileClient(string splitsFilePath,
            string segmentsFilePath,
            ISegmentCache segmentCacheInstance = null,
            IFeatureFlagCache featureFlagCacheInstance = null,
            IImpressionsLog impressionsLog = null,
            bool isLabelsEnabled = true,
            IEventsLog eventsLog = null,
            ITrafficTypeValidator trafficTypeValidator = null,
            IImpressionsManager impressionsManager = null) : base("localhost")
        {
            _segmentCache = segmentCacheInstance ?? new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());

            var segmentFetcher = new JSONFileSegmentFetcher(segmentsFilePath, _segmentCache);
            var splitChangeFetcher = new JSONFileSplitChangeFetcher(splitsFilePath);
            var task = splitChangeFetcher.FetchAsync(-1, new FetchOptions());
            task.Wait();
            
            var splitChangesResult = task.Result;
            var parsedSplits = new ConcurrentDictionary<string, ParsedSplit>();

            _splitParser = new InMemorySplitParser(segmentFetcher, _segmentCache);

            foreach (var split in splitChangesResult.splits)
            {
                parsedSplits.TryAdd(split.name, _splitParser.Parse(split));
            }

            BuildFlagSetsFilter(new HashSet<string>());

            _featureFlagCache = featureFlagCacheInstance ?? new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(parsedSplits), _flagSetsFilter);
            _impressionsLog = impressionsLog;
            _eventsLog = eventsLog;
            _trafficTypeValidator = trafficTypeValidator;
            _blockUntilReadyService = new NoopBlockUntilReadyService();
            _manager = new SplitManager(_featureFlagCache, _blockUntilReadyService);
            _evaluator = new Evaluator.Evaluator(_featureFlagCache, new Splitter(), null);
            _uniqueKeysTracker = new NoopUniqueKeysTracker();
            _impressionsCounter = new NoopImpressionsCounter();
            _impressionsObserver = new NoopImpressionsObserver();
            _impressionsManager = impressionsManager ?? new ImpressionsManager(impressionsLog, null, _impressionsCounter, false, ImpressionsMode.Debug, null, _tasksManager, _uniqueKeysTracker, _impressionsObserver, isLabelsEnabled);

            BuildClientExtension();
        }

        #region Public Methods
        public void RemoveSplitFromCache(string splitName)
        {
            _featureFlagCache.Update(new List<ParsedSplit>(), new List<string> { splitName }, -1);
        }

        public void RemoveKeyFromSegmentCache(string segmentName, List<string> keys)
        {
            _segmentCache.RemoveFromSegment(segmentName, keys);
        }

        public override void Destroy()
        {
            if (_statusManager.IsDestroyed()) return;

            _factoryInstantiationsService.Decrease(ApiKey);
            _statusManager.SetDestroy();
            _featureFlagCache.Clear();
            _segmentCache.Clear();
        }

        public override Task DestroyAsync()
        {
            Destroy();
            return Task.FromResult(0);
        }
        #endregion
    }
}
