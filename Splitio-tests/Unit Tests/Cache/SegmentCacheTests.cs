using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class SegmentCacheTests
    {
        [TestMethod]
        public void RegisterSegmentTest()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var keys = new List<string> { "abcd", "1234" };
            var segmentName = "test";

            //Act
            segmentCache.AddToSegment(segmentName, keys);
            var result = segmentCache.IsInSegment(segmentName, "abcd");
            
            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsNotInSegmentTest()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var keys = new List<string> { "1234" };
            var segmentName = "test";

            //Act
            segmentCache.AddToSegment(segmentName, keys);
            var result = segmentCache.IsInSegment(segmentName, "abcd");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsInSegmentWithInexistentSegmentTest()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());

            //Act
            var result = segmentCache.IsInSegment("test", "abcd");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RemoveKeyFromSegmentTest()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var keys = new List<string> { "1234" };
            var segmentName = "test";

            //Act
            segmentCache.AddToSegment(segmentName, keys);
            var result = segmentCache.IsInSegment(segmentName, "1234");
            segmentCache.RemoveFromSegment(segmentName, keys);
            var result2 = segmentCache.IsInSegment(segmentName, "1234");

            //Assert
            Assert.IsTrue(result);
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public void SetAndGetChangeNumberTest()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var segmentName = "test";

            //Act
            segmentCache.AddToSegment(segmentName, null);
            segmentCache.SetChangeNumber(segmentName, 1234);
            var result = segmentCache.GetChangeNumber(segmentName);

            //Assert
            Assert.AreEqual(1234, result);
        }

        [TestMethod]
        public void GetSegmentKeysTest()
        {
            // Arrange.
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var keys = new List<string> { "abcd", "1234" };
            var segmentName = "test";
            segmentCache.AddToSegment(segmentName, keys);

            // Act & Assert.
            var result = segmentCache.GetSegmentKeys(segmentName);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("abcd"));
            Assert.IsTrue(result.Contains("1234"));

            var otherSegment = "segmentName";
            var otherKeys = new List<string>();
            result = segmentCache.GetSegmentKeys(otherSegment);
            Assert.AreEqual(0, result.Count);

            segmentCache.AddToSegment(otherSegment, otherKeys);
            result = segmentCache.GetSegmentKeys(otherSegment);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetSegmentNamesTest()
        {
            // Arrange.
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());

            // Act & Assert.
            var result = segmentCache.GetSegmentNames();
            Assert.AreEqual(0, result.Count);

            var keys = new List<string> { "abcd", "1234" };
            var segmentName = "test";
            segmentCache.AddToSegment(segmentName, keys);
            segmentCache.AddToSegment($"{segmentName}-1", keys);
            segmentCache.AddToSegment($"{segmentName}-2", keys);

            result = segmentCache.GetSegmentNames();
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(segmentName));
            Assert.IsTrue(result.Contains($"{segmentName}-1"));
            Assert.IsTrue(result.Contains($"{segmentName}-2"));
        }
    }
}
