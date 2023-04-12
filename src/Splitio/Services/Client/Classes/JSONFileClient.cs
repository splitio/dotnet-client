﻿using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
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
        public JSONFileClient(string splitsFilePath,
            string segmentsFilePath,
            ISegmentCache segmentCacheInstance = null,
            ISplitCache splitCacheInstance = null,
            IImpressionsLog impressionsLog = null,
            bool isLabelsEnabled = true,
            IEventsLog eventsLog = null,
            ITrafficTypeValidator trafficTypeValidator = null,
            IImpressionsManager impressionsManager = null) : base()
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

            _splitCache = splitCacheInstance ?? new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(parsedSplits));

            _impressionsLog = impressionsLog;

            LabelsEnabled = isLabelsEnabled;

            _eventsLog = eventsLog;
            _trafficTypeValidator = trafficTypeValidator;
            
            _blockUntilReadyService = new NoopBlockUntilReadyService();
            _manager = new SplitManager(_splitCache, _blockUntilReadyService);

            ApiKey = "localhost";

            BuildEvaluator();

            _uniqueKeysTracker = new NoopUniqueKeysTracker();
            _impressionsCounter = new NoopImpressionsCounter();
            _impressionsObserver = new NoopImpressionsObserver();
            _impressionsManager = impressionsManager ?? new ImpressionsManager(impressionsLog, null, _impressionsCounter, false, ImpressionsMode.Debug, null, _tasksManager, _uniqueKeysTracker, _impressionsObserver);
        }

        #region Public Methods
        public void RemoveSplitFromCache(string splitName)
        {
            _splitCache.RemoveSplit(splitName);
        }

        public async Task RemoveKeyFromSegmentCacheAsync(string segmentName, List<string> keys)
        {
            await _segmentCache.RemoveFromSegmentAsync(segmentName, keys);
        }

        public override void Destroy()
        {
            if (!_statusManager.IsDestroyed())
            {
                _splitCache.Clear();
                _segmentCache.Clear();
                base.Destroy();
            }
        }
        #endregion
    }
}
