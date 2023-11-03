using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
using Splitio.Services.Filters;
using Splitio.Services.Parsing.Classes;
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
    public class SelfRefreshingSplitFetcherTests
    {
        private readonly string rootFilePath;

        public SelfRefreshingSplitFetcherTests()
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
            var splitParser = new InMemorySplitParser(new JSONFileSegmentFetcher($"{rootFilePath}segment_payed.json", segmentCache), segmentCache);
            var splitChangeFetcher = new JSONFileSplitChangeFetcher($"{rootFilePath}splits_staging.json");
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>());
            var gates = new InMemoryReadinessGatesCache();
            var taskManager = new TasksManager(gates);
            var task = taskManager.NewPeriodicTask(Splitio.Enums.Task.FeatureFlagsFetcher, 250);
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var featureFlagSyncService = new FeatureFlagSyncService(splitParser, splitCache, flagSetsFilter);
            var selfRefreshingSplitFetcher = new SelfRefreshingSplitFetcher(splitChangeFetcher, gates, task, splitCache, featureFlagSyncService);
            selfRefreshingSplitFetcher.Start();
            Thread.Sleep(500);

            //Act           
            var result = splitCache.GetSplit("Pato_Test_1");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.name == "Pato_Test_1");
            Assert.IsTrue(result.conditions.Count > 0);

            await selfRefreshingSplitFetcher.StopAsync();
            selfRefreshingSplitFetcher.Clear();
        }

        [TestMethod]
        [DeploymentItem(@"Resources\splits_staging_4.json")]
        [DeploymentItem(@"Resources\segment_payed.json")]
        public async Task ExecuteGetSuccessfulWithResultsFromJSONFileIncludingTrafficAllocation()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var splitParser = new InMemorySplitParser(new JSONFileSegmentFetcher($"{rootFilePath}segment_payed.json", segmentCache), segmentCache);
            var splitChangeFetcher = new JSONFileSplitChangeFetcher($"{rootFilePath}splits_staging_4.json");
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>());
            var gates = new InMemoryReadinessGatesCache();
            var taskManager = new TasksManager(gates);
            var task = taskManager.NewPeriodicTask(Splitio.Enums.Task.FeatureFlagsFetcher, 250);
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var featureFlagSyncService = new FeatureFlagSyncService(splitParser, splitCache, flagSetsFilter);
            var selfRefreshingSplitFetcher = new SelfRefreshingSplitFetcher(splitChangeFetcher, gates, task, splitCache, featureFlagSyncService);
            selfRefreshingSplitFetcher.Start();
            Thread.Sleep(500);

            //Act           
            var result = splitCache.GetSplit("Traffic_Allocation_UI");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.name == "Traffic_Allocation_UI");
            Assert.IsTrue(result.trafficAllocation == 100);
            Assert.IsTrue(result.trafficAllocationSeed == 0);
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
            var sdkApiClient = new SplitSdkApiClient(httpClient, telemetryStorage, baseUrl, flagSetsFilter);
            var apiSplitChangeFetcher = new ApiSplitChangeFetcher(sdkApiClient);
            var sdkSegmentApiClient = new SegmentSdkApiClient(httpClient, telemetryStorage, baseUrl);
            var apiSegmentChangeFetcher = new ApiSegmentChangeFetcher(sdkSegmentApiClient);
            var gates = new InMemoryReadinessGatesCache();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var segmentTaskQueue = new BlockingCollection<SelfRefreshingSegment>(new ConcurrentQueue<SelfRefreshingSegment>());
            var taskManager = new TasksManager(gates);
            var workerTask = taskManager.NewPeriodicTask(Splitio.Enums.Task.SegmentsWorkerFetcher, 0);
            var worker = new SegmentTaskWorker(4, segmentTaskQueue, gates, workerTask, taskManager);
            var segmentsTask = taskManager.NewPeriodicTask(Splitio.Enums.Task.SegmentsFetcher, 3000);
            var selfRefreshingSegmentFetcher = new SelfRefreshingSegmentFetcher(apiSegmentChangeFetcher, segmentCache, segmentTaskQueue, segmentsTask, worker, gates);
            var splitParser = new InMemorySplitParser(selfRefreshingSegmentFetcher, segmentCache);
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>());
            var task = taskManager.NewPeriodicTask(Splitio.Enums.Task.FeatureFlagsFetcher, 3000);
            var featureFlagSyncService = new FeatureFlagSyncService(splitParser, splitCache, flagSetsFilter);
            var selfRefreshingSplitFetcher = new SelfRefreshingSplitFetcher(apiSplitChangeFetcher, gates, task, splitCache, featureFlagSyncService);
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
