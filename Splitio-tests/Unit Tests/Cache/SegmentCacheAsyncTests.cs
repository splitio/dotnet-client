using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class SegmentCacheAsyncTests
    {
        private readonly ISegmentCache _cache;

        public SegmentCacheAsyncTests()
        {
            var segments = new ConcurrentDictionary<string, Segment>();

            _cache = new InMemorySegmentCache(segments);
        }

        [TestMethod]
        public async Task ExecuteAddToSegmentAsyncSuccessful()
        {
            // Arrange
            var segmentName = "segment-name";
            var keys = new List<string>() { "key-1", "key-2" };

            // Act
            await _cache.AddToSegmentAsync(segmentName, keys);

            // Assert
            var result = await _cache.GetSegmentKeysAsync(segmentName);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("key-1"));
            Assert.IsTrue(result.Contains("key-2"));
        }

        [TestMethod]
        public async Task ExecuteRemoveFromSegmentAsyncSuccessful()
        {
            // Arrange
            var segmentName = "segment-name";
            var keys = new List<string>() { "key-1", "key-2", "key-3" };

            await _cache.AddToSegmentAsync(segmentName, keys);

            // Act
            await _cache.RemoveFromSegmentAsync(segmentName, new List<string> { "key-2" });

            // Assert
            var result = await _cache.GetSegmentKeysAsync(segmentName);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("key-1"));
            Assert.IsFalse(result.Contains("key-2"));
            Assert.IsTrue(result.Contains("key-3"));
        }

        [TestMethod]
        public async Task ExecuteIsInSegmentAsyncSuccessful()
        {
            // Arrange
            var segmentName = "segment-name";
            var keys = new List<string>() { "key-1", "key-2", "key-3" };

            await _cache.AddToSegmentAsync(segmentName, keys);

            // Act & Assert
            Assert.IsTrue(await _cache.IsInSegmentAsync(segmentName, "key-3"));
            Assert.IsFalse(await _cache.IsInSegmentAsync(segmentName, "key-4"));
        }

        [TestMethod]
        public async Task ExecuteSetChangeNumberAsyncSuccessful()
        {
            // Arrange
            var segmentName = "segment-name";
            var keys = new List<string>() { "key-1", "key-2", "key-3" };

            await _cache.AddToSegmentAsync(segmentName, keys);

            // Act
            await _cache.SetChangeNumberAsync(segmentName, 150);

            // Assert
            Assert.AreEqual(150, await _cache.GetChangeNumberAsync(segmentName));
            Assert.AreEqual(-1, await _cache.GetChangeNumberAsync("other-segment"));
        }

        [TestMethod]
        public async Task ExecuteGetSegmentNamesAsyncSuccessful()
        {
            // Arrange
            for (int i = 0; i < 3; i++)
            {
                var segmentName = "segment-name-" + i;
                var keys = new List<string>() { "key-1", "key-2", "key-3" };

                await _cache.AddToSegmentAsync(segmentName, keys);
            }

            // Act
            var result = await _cache.GetSegmentNamesAsync();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("segment-name-0"));
            Assert.IsTrue(result.Contains("segment-name-1"));
            Assert.IsTrue(result.Contains("segment-name-2"));
        }

        [TestMethod]
        public async Task ExecuteClearAsyncAsyncSuccessful()
        {
            // Arrange
            for (int i = 0; i < 3; i++)
            {
                var segmentName = "segment-name-" + i;
                var keys = new List<string>() { "key-1", "key-2", "key-3" };

                await _cache.AddToSegmentAsync(segmentName, keys);
            }

            // Act & Assert
            var result = await _cache.GetSegmentNamesAsync();
            Assert.AreEqual(3, result.Count);

            await _cache.ClearAsync();

            result = await _cache.GetSegmentNamesAsync();
            Assert.AreEqual(0, result.Count);
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            await _cache.ClearAsync();
        }
    }
}
