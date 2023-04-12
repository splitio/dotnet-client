using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Parsing;
using Splitio.Domain;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Splitio.Services.Cache.Classes;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests
{
    [TestClass]
    public class UserDefinedSegmentMatcherTests
    {
        [TestMethod]
        public async Task MatchShouldReturnTrueOnMatchingSegmentWithKey()
        {
            //Arrange
            var keys = new List<string>
            {
                "test1",
                "test2"
            };

            var segmentName = "test-segment";
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            await segmentCache.AddToSegmentAsync(segmentName, keys);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.Match(new Key("test2", "test2"));

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseOnNonMatchingSegmentWithKey()
        {
            //Arrange
            var keys = new List<string>
            {
                "test1",
                "test2"
            };

            var segmentName = "test-segment";
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            await segmentCache.AddToSegmentAsync(segmentName, keys);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.Match(new Key("test3", "test3"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseIfSegmentEmptyWithKey()
        {
            //Arrange
            var segmentName = "test-segment";
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            await segmentCache.AddToSegmentAsync(segmentName, null);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.Match(new Key("test2", "test2"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseIfCacheEmptyWithKey()
        {
            //Arrange
            var segmentName = "test-segment";
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = await matcher.Match(new Key("test2", "test2"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnTrueOnMatchingSegment()
        {
            //Arrange
            var keys = new List<string>
            {
                "test1",
                "test2"
            };

            var segmentName = "test-segment";
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            await segmentCache.AddToSegmentAsync(segmentName, keys);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = matcher.Match("test2");

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseOnNonMatchingSegment()
        {
            //Arrange
            var keys = new List<string>
            {
                "test1",
                "test2"
            };

            var segmentName = "test-segment";
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            await segmentCache.AddToSegmentAsync(segmentName, keys);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = matcher.Match("test3");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseIfSegmentEmpty()
        {
            //Arrange
            var segmentName = "test-segment";
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            await segmentCache.AddToSegmentAsync(segmentName, null);

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = matcher.Match("test2");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfCacheEmpty()
        {
            //Arrange
            var segmentName = "test-segment";
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());

            var matcher = new UserDefinedSegmentMatcher(segmentName, segmentCache);

            //Act
            var result = matcher.Match("test2");

            //Assert
            Assert.IsFalse(result);
        }
    }
}
