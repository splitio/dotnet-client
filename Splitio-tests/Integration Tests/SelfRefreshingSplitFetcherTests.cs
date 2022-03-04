using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Client.Classes;
using Splitio.Services.Parsing.Classes;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Classes;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

#if NETCORE
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        [DeploymentItem(@"Resources\splits_staging.json")]
        [DeploymentItem(@"Resources\segment_payed.json")]
        public void ExecuteGetSuccessfulWithResultsFromJSONFile()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var splitParser = new InMemorySplitParser(new JSONFileSegmentFetcher($"{rootFilePath}segment_payed.json", segmentCache), segmentCache);
            var splitChangeFetcher = new JSONFileSplitChangeFetcher($"{rootFilePath}splits_staging.json");
            var splitChangesResult = splitChangeFetcher.Fetch(-1, new FetchOptions());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>());
            var gates = new InMemoryReadinessGatesCache();
            var wrapperAdapter = new WrapperAdapter();
            var selfRefreshingSplitFetcher = new SelfRefreshingSplitFetcher(splitChangeFetcher, splitParser, gates, 30, new TasksManager(wrapperAdapter), splitCache);
            selfRefreshingSplitFetcher.Start();
            gates.WaitUntilReady(1000);

            //Act           
            ParsedSplit result = (ParsedSplit)splitCache.GetSplit("Pato_Test_1");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.name == "Pato_Test_1");
            Assert.IsTrue(result.conditions.Count > 0);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\splits_staging_4.json")]
        [DeploymentItem(@"Resources\segment_payed.json")]
        public void ExecuteGetSuccessfulWithResultsFromJSONFileIncludingTrafficAllocation()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var splitParser = new InMemorySplitParser(new JSONFileSegmentFetcher($"{rootFilePath}segment_payed.json", segmentCache), segmentCache);
            var splitChangeFetcher = new JSONFileSplitChangeFetcher($"{rootFilePath}splits_staging_4.json");
            var splitChangesResult = splitChangeFetcher.Fetch(-1, new FetchOptions());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>());
            var gates = new InMemoryReadinessGatesCache();
            var wrapperAdapter = new WrapperAdapter();
            var selfRefreshingSplitFetcher = new SelfRefreshingSplitFetcher(splitChangeFetcher, splitParser, gates, 30, new TasksManager(wrapperAdapter), splitCache);
            selfRefreshingSplitFetcher.Start();
            gates.WaitUntilReady(1000);

            //Act           
            ParsedSplit result = (ParsedSplit)splitCache.GetSplit("Traffic_Allocation_UI");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.name == "Traffic_Allocation_UI");
            Assert.IsTrue(result.trafficAllocation == 100);
            Assert.IsTrue(result.trafficAllocationSeed == 0);
            Assert.IsTrue(result.conditions.Count > 0);
            Assert.IsNotNull(result.conditions.Find(x => x.conditionType == ConditionType.ROLLOUT));
        }

        [TestMethod]
        [Ignore]
        public void ExecuteGetSuccessfulWithResults()
        {
            //Arrange
            var baseUrl = "https://sdk-aws-staging.split.io/api/";
            //var baseUrl = "http://localhost:3000/api/";
            var headers = new Dictionary<string, string>
            {
                { "SplitSDKMachineIP", "1.0.0.0" },
                { "SplitSDKMachineName", "localhost" },
                { "SplitSDKVersion", "1" }
            };

            var telemetryStorage = new InMemoryTelemetryStorage();
            var sdkApiClient = new SplitSdkApiClient("///PUT API KEY HERE///", headers, baseUrl, 10000, 10000, telemetryStorage);
            var apiSplitChangeFetcher = new ApiSplitChangeFetcher(sdkApiClient);
            var sdkSegmentApiClient = new SegmentSdkApiClient("///PUT API KEY HERE///", headers, baseUrl, 10000, 10000, telemetryStorage);
            var statusManager = new InMemoryReadinessGatesCache();
            var wrapperAdapter = new WrapperAdapter();

            var config = new SegmentFetcherConfig
            {
                Interval = 30,
                NumberOfParallelSegments = 4,
                SegmentChangeFetcher = new ApiSegmentChangeFetcher(sdkSegmentApiClient),
                SegmentsCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>()),
                SegmentTaskQueue = new SegmentTaskQueue(),
                StatusManager = statusManager,
                TasksManager = new TasksManager(wrapperAdapter),
                WrapperAdapter = wrapperAdapter
            };

            var selfRefreshingSegmentFetcher = new SelfRefreshingSegmentFetcher(config);

            var splitParser = new InMemorySplitParser(selfRefreshingSegmentFetcher, config.SegmentsCache);
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>());
            var selfRefreshingSplitFetcher = new SelfRefreshingSplitFetcher(apiSplitChangeFetcher, splitParser, statusManager, 30, new TasksManager(wrapperAdapter), splitCache);
            selfRefreshingSplitFetcher.Start();

            //Act           
            statusManager.WaitUntilReady(1000);
            selfRefreshingSplitFetcher.Stop();
            ParsedSplit result = splitCache.GetSplit("Pato_Test_1");
            ParsedSplit result2 = splitCache.GetSplit("Manu_Test_1");
            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.name == "Pato_Test_1");
            Assert.IsTrue(result.conditions.Count > 0);

        }

        [TestMethod]
        public void ExecuteGetWithoutResults()
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
            var sdkApiClient = new SplitSdkApiClient("0", headers, baseUrl, 10000, 10000, telemetryStorage);
            var apiSplitChangeFetcher = new ApiSplitChangeFetcher(sdkApiClient);
            var sdkSegmentApiClient = new SegmentSdkApiClient("0", headers, baseUrl, 10000, 10000, telemetryStorage);
            var statusManager = new InMemoryReadinessGatesCache();

            var wrapperAdapter = new WrapperAdapter();
            var config = new SegmentFetcherConfig
            {
                Interval = 30,
                NumberOfParallelSegments = 4,
                SegmentChangeFetcher = new ApiSegmentChangeFetcher(sdkSegmentApiClient),
                SegmentsCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>()),
                SegmentTaskQueue = new SegmentTaskQueue(),
                StatusManager = statusManager,
                TasksManager = new TasksManager(wrapperAdapter),
                WrapperAdapter = wrapperAdapter
            };

            var selfRefreshingSegmentFetcher = new SelfRefreshingSegmentFetcher(config);
            var splitParser = new InMemorySplitParser(selfRefreshingSegmentFetcher, config.SegmentsCache);
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>());
            var selfRefreshingSplitFetcher = new SelfRefreshingSplitFetcher(apiSplitChangeFetcher, splitParser, statusManager, 30, new TasksManager(wrapperAdapter), splitCache);
            selfRefreshingSplitFetcher.Start();

            //Act
            statusManager.WaitUntilReady(10);

            var result = splitCache.GetSplit("condition_and");

            //Assert
            Assert.IsNull(result);
        }
    }
}
