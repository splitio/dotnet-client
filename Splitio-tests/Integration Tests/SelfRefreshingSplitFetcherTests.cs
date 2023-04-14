using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Client.Classes;
using Splitio.Services.Common;
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

#if NET_LATEST
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
            //var splitChangesResult = await splitChangeFetcher.FetchAsync(-1, new FetchOptions());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>());
            var gates = new InMemoryReadinessGatesCache();
            var wrapperAdapter = WrapperAdapter.Instance();
            var selfRefreshingSplitFetcher = new SelfRefreshingSplitFetcher(splitChangeFetcher, splitParser, gates, 30, new TasksManager(wrapperAdapter), splitCache);
            selfRefreshingSplitFetcher.Start();
            gates.WaitUntilReady(1000);

            //Act           
            var result = splitCache.GetSplit("Pato_Test_1");

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
            //var splitChangesResult = await splitChangeFetcher.FetchAsync(-1, new FetchOptions());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>());
            var gates = new InMemoryReadinessGatesCache();
            var wrapperAdapter = WrapperAdapter.Instance();
            var selfRefreshingSplitFetcher = new SelfRefreshingSplitFetcher(splitChangeFetcher, splitParser, gates, 30, new TasksManager(wrapperAdapter), splitCache);
            selfRefreshingSplitFetcher.Start();
            gates.WaitUntilReady(1000);

            //Act           
            var result = splitCache.GetSplit("Traffic_Allocation_UI");

            //Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.name == "Traffic_Allocation_UI");
            Assert.IsTrue(result.trafficAllocation == 100);
            Assert.IsTrue(result.trafficAllocationSeed == 0);
            Assert.IsTrue(result.conditions.Count > 0);
            Assert.IsNotNull(result.conditions.Find(x => x.ConditionType == ConditionType.ROLLOUT));
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
            var config = new SelfRefreshingConfig
            {
                HttpConnectionTimeout = 10000,
                HttpReadTimeout = 10000
            };
            var httpClient = new SplitioHttpClient("0", config, headers);
            var sdkApiClient = new SplitSdkApiClient(httpClient, telemetryStorage, baseUrl);
            var apiSplitChangeFetcher = new ApiSplitChangeFetcher(sdkApiClient);
            var sdkSegmentApiClient = new SegmentSdkApiClient(httpClient, telemetryStorage, baseUrl);
            var apiSegmentChangeFetcher = new ApiSegmentChangeFetcher(sdkSegmentApiClient);
            var gates = new InMemoryReadinessGatesCache();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var segmentTaskQueue = new SegmentTaskQueue();
            var wrapperAdapter = WrapperAdapter.Instance();
            var selfRefreshingSegmentFetcher = new SelfRefreshingSegmentFetcher(apiSegmentChangeFetcher, gates, 30, segmentCache, 4, segmentTaskQueue, new TasksManager(wrapperAdapter), wrapperAdapter);
            var splitParser = new InMemorySplitParser(selfRefreshingSegmentFetcher, segmentCache);
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>());
            var selfRefreshingSplitFetcher = new SelfRefreshingSplitFetcher(apiSplitChangeFetcher, splitParser, gates, 30, new TasksManager(wrapperAdapter), splitCache);
            selfRefreshingSplitFetcher.Start();

            //Act
            gates.WaitUntilReady(10);

            var result = splitCache.GetSplit("condition_and");

            //Assert
            Assert.IsNull(result);
        }
    }
}
