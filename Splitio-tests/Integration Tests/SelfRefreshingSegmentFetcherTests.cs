using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Client.Classes;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Splitio_Tests.Integration_Tests
{
    [TestClass]
    public class SelfRefreshingSegmentFetcherTests
    {
        private readonly string rootFilePath;

        public SelfRefreshingSegmentFetcherTests()
        {
            // This line is to clean the warnings.
            rootFilePath = string.Empty;

#if NETCORE
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        [DeploymentItem(@"Resources\segment_payed.json")]
        public void ExecuteGetSuccessfulWithResultsFromJSONFile()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());

            var segmentFetcher = new JSONFileSegmentFetcher($"{rootFilePath}segment_payed.json", segmentCache);

            //Act
            segmentFetcher.InitializeSegment("payed");

            //Assert
            Assert.IsTrue(segmentCache.IsInSegment("payed", "abcdz"));
        }


        [TestMethod]
        [Ignore] 
        public void ExecuteGetSuccessfulWithResults()
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
            var sdkApiClient = new SegmentSdkApiClient("///PUT API KEY HERE///", headers, baseUrl, 10000, 10000, telemetryStorage);
            var apiSegmentChangeFetcher = new ApiSegmentChangeFetcher(sdkApiClient);
            var gates = new InMemoryReadinessGatesCache();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var segmentTaskQueue = new SegmentTaskQueue();
            var wrapperAdapter = new WrapperAdapter();
            var selfRefreshingSegmentFetcher = new SelfRefreshingSegmentFetcher(apiSegmentChangeFetcher, gates, 30, segmentCache, 4, segmentTaskQueue, new TasksManager(wrapperAdapter), wrapperAdapter);

            //Act
            var name = "payed";
            selfRefreshingSegmentFetcher.InitializeSegment(name);

            while(!gates.AreSegmentsReady(1000))
            {
                Thread.Sleep(10);
            }

            //Assert
            Assert.IsTrue(segmentCache.IsInSegment(name, "abcdz"));
        }

    }
}
