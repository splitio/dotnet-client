using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
using Splitio.Services.Filters;
using Splitio.Services.Parsing;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Classes;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class TargetingRulesFetcherTests
    {
        private readonly string rootFilePath;

        public TargetingRulesFetcherTests()
        {
            // This line is to clean the warnings.
            rootFilePath = string.Empty;

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        [DeploymentItem(@"Resources\splits_staging.json")]
        [DeploymentItem(@"Resources\segment_payed.json")]
        public async Task ExecuteGetSuccessfulWithResultsFromJSONFile()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var rbsCache = new InMemoryRuleBasedSegmentCache(new ConcurrentDictionary<string, RuleBasedSegment>());
            var segmentFetcher = new JSONFileSegmentFetcher($"{rootFilePath}segment_payed.json", segmentCache);
            var splitParser = new FeatureFlagParser(segmentCache, segmentFetcher);
            var splitChangeFetcher = new JSONFileSplitChangeFetcher($"{rootFilePath}splits_staging.json");
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), flagSetsFilter);
            var gates = new InMemoryReadinessGatesCache();
            var taskManager = new TasksManager(gates);
            var task = taskManager.NewPeriodicTask(Splitio.Enums.Task.FeatureFlagsFetcher, 250);
            var featureFlagSyncService = new FeatureFlagUpdater(splitParser, splitCache, flagSetsFilter, rbsCache);
            var rbsParser = new RuleBasedSegmentParser(segmentCache, segmentFetcher);
            var rbsUpdater = new RuleBasedSegmentUpdater(rbsParser, rbsCache);
            var targetingRulesFetcher = new TargetingRulesFetcher(splitChangeFetcher, gates, task, splitCache, featureFlagSyncService, rbsUpdater, rbsCache);
            targetingRulesFetcher.Start();
            Thread.Sleep(1000);

            //Act
            var ffResult = splitCache.GetSplit("Pato_Test_1");
            var ffCn = splitCache.GetChangeNumber();

            var rbsResult = rbsCache.Get("rbs_test");
            var rbsCn = rbsCache.GetChangeNumber();

            //Assert
            Assert.IsNotNull(ffResult);
            Assert.AreEqual("Pato_Test_1", ffResult.name);
            Assert.IsTrue(ffResult.conditions.Count > 0);
            Assert.AreEqual(1470855828956, ffCn);

            Assert.IsNotNull(rbsResult);
            Assert.AreEqual("rbs_test", rbsResult.Name);
            Assert.IsTrue(rbsResult.CombiningMatchers.Count > 0);
            Assert.AreEqual(10, rbsCn);

            await targetingRulesFetcher.StopAsync();
            targetingRulesFetcher.Clear();
        }

        [TestMethod]
        [DeploymentItem(@"Resources\splits_staging_4.json")]
        [DeploymentItem(@"Resources\segment_payed.json")]
        public async Task ExecuteGetSuccessfulWithResultsFromJSONFileIncludingTrafficAllocation()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var rbsCache = new InMemoryRuleBasedSegmentCache(new ConcurrentDictionary<string, RuleBasedSegment>());
            var segmentFetcher = new JSONFileSegmentFetcher($"{rootFilePath}segment_payed.json", segmentCache);
            var splitParser = new FeatureFlagParser(segmentCache, segmentFetcher);
            var splitChangeFetcher = new JSONFileSplitChangeFetcher($"{rootFilePath}splits_staging_4.json");
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), flagSetsFilter);
            var gates = new InMemoryReadinessGatesCache();
            var taskManager = new TasksManager(gates);
            var task = taskManager.NewPeriodicTask(Splitio.Enums.Task.FeatureFlagsFetcher, 250);
            var featureFlagSyncService = new FeatureFlagUpdater(splitParser, splitCache, flagSetsFilter, rbsCache);
            var rbsParser = new RuleBasedSegmentParser(segmentCache, segmentFetcher);
            var rbsUpdater = new RuleBasedSegmentUpdater(rbsParser, rbsCache);
            var selfRefreshingSplitFetcher = new TargetingRulesFetcher(splitChangeFetcher, gates, task, splitCache, featureFlagSyncService, rbsUpdater, rbsCache);
            selfRefreshingSplitFetcher.Start();
            Thread.Sleep(1000);

            //Act           
            var result = splitCache.GetSplit("Traffic_Allocation_UI");

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Traffic_Allocation_UI", result.name);
            Assert.AreEqual(100, result.trafficAllocation);
            Assert.AreEqual(0, result.trafficAllocationSeed);
            Assert.IsTrue(result.conditions.Count > 0);
            Assert.IsNotNull(result.conditions.Find(x => x.conditionType == ConditionType.ROLLOUT));

            await selfRefreshingSplitFetcher.StopAsync();
            selfRefreshingSplitFetcher.Clear();
        }

        [TestMethod]
        public async Task ExecuteGetWithoutResults()
        {
            //Arrange
            var baseUrl = "https://sdk-aws-staging.split.io/api/";
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };

            var telemetryStorage = new InMemoryTelemetryStorage();
            var config = new SelfRefreshingConfig
            {
                HttpConnectionTimeout = 10000,
                HttpReadTimeout = 10000
            };
            var httpClient = new SplitioHttpClient("0", config, headers);
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var sdkApiClient = new SplitSdkApiClient(httpClient, telemetryStorage, baseUrl, flagSetsFilter, false);
            var apiSplitChangeFetcher = new ApiSplitChangeFetcher(sdkApiClient);
            var sdkSegmentApiClient = new SegmentSdkApiClient(httpClient, telemetryStorage, baseUrl);
            var apiSegmentChangeFetcher = new ApiSegmentChangeFetcher(sdkSegmentApiClient);
            var gates = new InMemoryReadinessGatesCache();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var segmentsQueue = new SplitQueue<SelfRefreshingSegment>();
            var taskManager = new TasksManager(gates);
            var worker = new SegmentTaskWorker(4, segmentsQueue);
            segmentsQueue.AddObserver(worker);
            var segmentsTask = taskManager.NewPeriodicTask(Splitio.Enums.Task.SegmentsFetcher, 3000);
            var segmentFetcher = new SelfRefreshingSegmentFetcher(apiSegmentChangeFetcher, segmentCache, segmentsQueue, segmentsTask, gates);
            var rbsCache = new InMemoryRuleBasedSegmentCache(new ConcurrentDictionary<string, RuleBasedSegment>());
            var splitParser = new FeatureFlagParser(segmentCache, segmentFetcher);
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), flagSetsFilter);
            var task = taskManager.NewPeriodicTask(Splitio.Enums.Task.FeatureFlagsFetcher, 3000);
            var featureFlagSyncService = new FeatureFlagUpdater(splitParser, splitCache, flagSetsFilter, rbsCache);
            var rbsParser = new RuleBasedSegmentParser(segmentCache, segmentFetcher);
            var rbsUpdater = new RuleBasedSegmentUpdater(rbsParser, rbsCache);
            var selfRefreshingSplitFetcher = new TargetingRulesFetcher(apiSplitChangeFetcher, gates, task, splitCache, featureFlagSyncService, rbsUpdater, rbsCache);
            selfRefreshingSplitFetcher.Start();

            //Act
            gates.WaitUntilReady(10);

            var result = splitCache.GetSplit("condition_and");

            //Assert
            Assert.IsNull(result);

            await selfRefreshingSplitFetcher.StopAsync();
            selfRefreshingSplitFetcher.Clear();
        }
    }
}
