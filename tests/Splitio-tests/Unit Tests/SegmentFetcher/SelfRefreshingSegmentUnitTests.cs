using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.SegmentFetcher.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.SegmentFetcher
{
    [TestClass]
    public class SelfRefreshingSegmentUnitTests
    {
        [TestMethod]
        public async Task FetchSegmentNullChangesFetcherResponseShouldNotUpdateCache()
        {
            //Arrange
            var apiClient = new Mock<ISegmentSdkApiClient>();
            var statusManager = new Mock<IStatusManager>();
            var apiFetcher = new ApiSegmentChangeFetcher(apiClient.Object);
            var segments  = new ConcurrentDictionary<string, Segment>();
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var cache = new InMemorySegmentCache(segments, internalEventsTask.Object);
            var segmentFetcher = new SelfRefreshingSegment("payed", apiFetcher, cache, statusManager.Object);

            apiClient
                .Setup(x => x.FetchSegmentChangesAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<FetchOptions>()))
                .Throws(new Exception());

            //Act
            await segmentFetcher.FetchSegmentAsync(new FetchOptions());

            //Assert
            Assert.AreEqual(0, segments.Count);
        }

        [TestMethod]
        public async Task FetchSegmentShouldUpdateSegmentsCache()
        {
            //Arrange
            var apiClient = new Mock<ISegmentSdkApiClient>();
            var statusManager = new Mock<IStatusManager>();
            var apiFetcher = new ApiSegmentChangeFetcher(apiClient.Object);
            var segments = new ConcurrentDictionary<string, Segment>();
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var cache = new InMemorySegmentCache(segments, internalEventsTask.Object);
            var segmentFetcher = new SelfRefreshingSegment("payed", apiFetcher, cache, statusManager.Object);

            apiClient
                .Setup(x => x.FetchSegmentChangesAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<FetchOptions>()))
                .ReturnsAsync(@"{
                          'name': 'payed',
                          'added': [
                            'abcdz',
                            'bcadz',
                            'xzydz'
                          ],
                          'removed': [],
                          'since': -1,
                          'till': 10001
                        }");

            //Act
            await segmentFetcher.FetchSegmentAsync(new FetchOptions());

            //Assert
            Assert.AreEqual(1, segments.Count);
        }
    }
}
