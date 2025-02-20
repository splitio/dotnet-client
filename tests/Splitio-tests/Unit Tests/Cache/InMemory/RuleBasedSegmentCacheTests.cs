using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache.InMemory
{
    [TestClass]
    public class RuleBasedSegmentCacheTests
    {
        private InMemoryRuleBasedSegmentCache _segmentCache;
        private ConcurrentDictionary<string, RuleBasedSegment> _cache;

        [TestInitialize]
        public void Setup()
        {
            _cache = new ConcurrentDictionary<string, RuleBasedSegment>();
            _segmentCache = new InMemoryRuleBasedSegmentCache(_cache);
        }

        [TestMethod]
        public void Get_ShouldReturnSegmentIfExists()
        {
            // Arrange
            var segmentName = "test-segment";
            var segment = new RuleBasedSegment { Name = segmentName };
            _cache.TryAdd(segmentName, segment);

            // Act
            var result = _segmentCache.Get(segmentName);

            // Assert
            Assert.AreEqual(segment, result);
        }

        [TestMethod]
        public void Get_ShouldReturnNullIfSegmentDoesNotExist()
        {
            // Arrange
            var segmentName = "non-existent-segment";

            // Act
            var result = _segmentCache.Get(segmentName);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetChangeNumber_ShouldReturnChangeNumber()
        {
            // Arrange
            var changeNumber = 12345;
            _segmentCache.SetChangeNumber(changeNumber);

            // Act
            var result = _segmentCache.GetChangeNumber();

            // Assert
            Assert.AreEqual(changeNumber, result);
        }

        [TestMethod]
        public void Update_ShouldAddAndRemoveSegments()
        {
            // Arrange
            var segmentToAdd = new RuleBasedSegment { Name = "segment-to-add" };
            var segmentToRemove = "segment-to-remove";
            _cache.TryAdd(segmentToRemove, new RuleBasedSegment { Name = segmentToRemove });
            var till = 67890;

            // Act
            _segmentCache.Update(new List<RuleBasedSegment> { segmentToAdd }, new List<string> { segmentToRemove }, till);

            // Assert
            Assert.IsTrue(_cache.ContainsKey(segmentToAdd.Name));
            Assert.IsFalse(_cache.ContainsKey(segmentToRemove));
            Assert.AreEqual(till, _segmentCache.GetChangeNumber());
        }

        [TestMethod]
        public void Clear_ShouldRemoveAllSegments()
        {
            // Arrange
            _cache.TryAdd("segment1", new RuleBasedSegment { Name = "segment1" });
            _cache.TryAdd("segment2", new RuleBasedSegment { Name = "segment2" });

            // Act
            _segmentCache.Clear();

            // Assert
            Assert.AreEqual(0, _cache.Count);
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnSegmentIfExists()
        {
            // Arrange
            var segmentName = "test-segment";
            var segment = new RuleBasedSegment { Name = segmentName };
            _cache.TryAdd(segmentName, segment);

            // Act
            var result = await _segmentCache.GetAsync(segmentName);

            // Assert
            Assert.AreEqual(segment, result);
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnNullIfSegmentDoesNotExist()
        {
            // Arrange
            var segmentName = "non-existent-segment";

            // Act
            var result = await _segmentCache.GetAsync(segmentName);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetChangeNumberAsync_ShouldReturnChangeNumber()
        {
            // Arrange
            var changeNumber = 12345;
            _segmentCache.SetChangeNumber(changeNumber);

            // Act
            var result = await _segmentCache.GetChangeNumberAsync();

            // Assert
            Assert.AreEqual(changeNumber, result);
        }
    }
}
