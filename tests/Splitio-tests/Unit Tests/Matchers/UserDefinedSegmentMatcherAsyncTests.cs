using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Parsing;
using Splitio.Services.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class UserDefinedSegmentMatcherAsyncTests
    {
        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingSegmentWithKey()
        {
            //Arrange
            var keys = new List<string>
            {
                "test1",
                "test2"
            };

            var segmentName = "test-segment";
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);
            segmentCache.AddToSegment(segmentName, keys);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.MatchAsync(new Key("test2", "test2"));

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingSegmentWithKey()
        {
            //Arrange
            var keys = new List<string>
            {
                "test1",
                "test2"
            };

            var segmentName = "test-segment";
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);
            segmentCache.AddToSegment(segmentName, keys);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.MatchAsync(new Key("test3", "test3"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfSegmentEmptyWithKey()
        {
            //Arrange
            var segmentName = "test-segment";
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);
            segmentCache.AddToSegment(segmentName, null);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.MatchAsync(new Key("test2", "test2"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfCacheEmptyWithKey()
        {
            //Arrange
            var segmentName = "test-segment";
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.MatchAsync(new Key("test2", "test2"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingSegment()
        {
            //Arrange
            var keys = new List<string>
            {
                "test1",
                "test2"
            };

            var segmentName = "test-segment";
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);
            segmentCache.AddToSegment(segmentName, keys);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.MatchAsync("test2");

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingSegment()
        {
            //Arrange
            var keys = new List<string>
            {
                "test1",
                "test2"
            };

            var segmentName = "test-segment";
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);
            segmentCache.AddToSegment(segmentName, keys);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.MatchAsync("test3");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfSegmentEmpty()
        {
            //Arrange
            var segmentName = "test-segment";
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);
            segmentCache.AddToSegment(segmentName, null);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.MatchAsync("test2");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfCacheEmpty()
        {
            //Arrange
            var segmentName = "test-segment";
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>(), internalEventsTask.Object);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.MatchAsync("test2");

            //Assert
            Assert.IsFalse(result);
        }
    }
}
