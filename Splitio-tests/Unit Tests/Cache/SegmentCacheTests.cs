using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class SegmentCacheTests
    {
        [TestMethod]
        public async Task RegisterSegmentTest()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var keys = new List<string> { "abcd", "1234" };
            var segmentName = "test";

            //Act
            await segmentCache.AddToSegmentAsync(segmentName, keys);
            var result = await segmentCache.IsInSegmentAsync(segmentName, "abcd");
            
            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task IsNotInSegmentTest()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var keys = new List<string> { "1234" };
            var segmentName = "test";

            //Act
            await segmentCache.AddToSegmentAsync(segmentName, keys);
            var result = await segmentCache.IsInSegmentAsync(segmentName, "abcd");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsInSegmentAsyncWithInexistentSegmentTest()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());

            //Act
            var result = await segmentCache.IsInSegmentAsync("test", "abcd");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task RemoveKeyFromSegmentTest()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var keys = new List<string> { "1234" };
            var segmentName = "test";

            //Act
            await segmentCache.AddToSegmentAsync(segmentName, keys);
            var result = await segmentCache.IsInSegmentAsync(segmentName, "1234");
            await segmentCache.RemoveFromSegmentAsync(segmentName, keys);
            var result2 = await segmentCache.IsInSegmentAsync(segmentName, "1234");

            //Assert
            Assert.IsTrue(result);
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public async Task SetAndGetChangeNumberTest()
        {
            //Arrange
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var segmentName = "test";

            //Act
            await segmentCache.AddToSegmentAsync(segmentName, null);
            await segmentCache.SetChangeNumberAsync(segmentName, 1234);
            var result = await segmentCache.GetChangeNumberAsync(segmentName);

            //Assert
            Assert.AreEqual(1234, result);
        }

        [TestMethod]
        public async Task GetSegmentKeysTest()
        {
            // Arrange.
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());
            var keys = new List<string> { "abcd", "1234" };
            var segmentName = "test";
            await segmentCache.AddToSegmentAsync(segmentName, keys);

            // Act & Assert.
            var result = await segmentCache.GetSegmentKeysAsync(segmentName);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("abcd"));
            Assert.IsTrue(result.Contains("1234"));

            var otherSegment = "segmentName";
            var otherKeys = new List<string>();
            result = await segmentCache.GetSegmentKeysAsync(otherSegment);
            Assert.AreEqual(0, result.Count);

            await segmentCache.AddToSegmentAsync(otherSegment, otherKeys);
            result = await segmentCache.GetSegmentKeysAsync(otherSegment);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task GetSegmentNamesTest()
        {
            // Arrange.
            var segmentCache = new InMemorySegmentCache(new ConcurrentDictionary<string, Segment>());

            // Act & Assert.
            var result = await segmentCache.GetSegmentNamesAsync();
            Assert.AreEqual(0, result.Count);

            var keys = new List<string> { "abcd", "1234" };
            var segmentName = "test";
            await segmentCache.AddToSegmentAsync(segmentName, keys);
            await segmentCache.AddToSegmentAsync($"{segmentName}-1", keys);
            await segmentCache.AddToSegmentAsync($"{segmentName}-2", keys);

            result = await segmentCache.GetSegmentNamesAsync();
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(segmentName));
            Assert.IsTrue(result.Contains($"{segmentName}-1"));
            Assert.IsTrue(result.Contains($"{segmentName}-2"));
        }
    }
}
