﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.SegmentFetcher.Classes;
using System.Collections.Concurrent;
using System.Threading.Tasks;

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

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        [DeploymentItem(@"Resources\segment_payed.json")]
        public async Task ExecuteGetSuccessfulWithResultsFromJSONFile()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());

            var segmentFetcher = new JSONFileSegmentFetcher($"{rootFilePath}segment_payed.json", segmentCache);

            //Act
            await segmentFetcher.InitializeSegmentAsync("payed");

            //Assert
            Assert.IsTrue(await segmentCache.IsInSegmentAsync("payed", "abcdz"));
        }
    }
}
