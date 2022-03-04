using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Client.Classes;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
            var wrapperAdapter = new WrapperAdapter();
            var config = new SegmentFetcherConfig
            {
                Interval = 30,
                NumberOfParallelSegments = 4,
                SegmentChangeFetcher = new ApiSegmentChangeFetcher(sdkApiClient),
                SegmentsCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>()),
                SegmentTaskQueue = new SegmentTaskQueue(),
                StatusManager = new InMemoryReadinessGatesCache(),
                TasksManager = new TasksManager(wrapperAdapter),
                WrapperAdapter = wrapperAdapter
            };
            var selfRefreshingSegmentFetcher = new SelfRefreshingSegmentFetcher(config);

            //Act
            var name = "payed";
            selfRefreshingSegmentFetcher.InitializeSegment(name);

            //Assert
            Assert.IsTrue(config.SegmentsCache.IsInSegment(name, "abcdz"));
        }

    }
}
