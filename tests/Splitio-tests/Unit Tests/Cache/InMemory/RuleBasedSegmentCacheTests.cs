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

        [TestInitialize]
        public void Setup()
        {
            var cache = new ConcurrentDictionary<string, RuleBasedSegment>();
            _segmentCache = new InMemoryRuleBasedSegmentCache(cache);
        }

        [TestMethod]
        public void Get_ShouldReturnSegmentIfExists()
        {
            // Arrange
            var segmentName = "test-segment";
            var segment = new RuleBasedSegment { Name = segmentName };

            _segmentCache.Update(new List<RuleBasedSegment> { segment }, new List<string>(), 10);

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
            var segmentToRemove = new RuleBasedSegment { Name = "segment-to-remove" };
            var till = 67890;

            // Act
            _segmentCache.Update(new List<RuleBasedSegment> { segmentToAdd, segmentToRemove }, new List<string> { segmentToRemove.Name }, till);

            // Assert
            Assert.IsTrue(_segmentCache.Contains(new List<string> { segmentToAdd.Name }));
            Assert.IsFalse(_segmentCache.Contains(new List<string> { segmentToRemove.Name }));
            Assert.AreEqual(till, _segmentCache.GetChangeNumber());
        }

        [TestMethod]
        public void Clear_ShouldRemoveAllSegments()
        {
            // Arrange
            var toAdd = new List<RuleBasedSegment>
            {
                new RuleBasedSegment { Name = "segment1" },
                new RuleBasedSegment { Name = "segment2" }
            };
            _segmentCache.Update(toAdd, new List<string>(), 10);

            // Act
            _segmentCache.Clear();

            // Assert
            Assert.IsFalse(_segmentCache.Contains(new List<string> { "segment1", "segment2" }));
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnSegmentIfExists()
        {
            // Arrange
            var segmentName = "test-segment";
            var segment = new RuleBasedSegment { Name = segmentName };
            _segmentCache.Update(new List<RuleBasedSegment> { segment }, new List<string>(), 10);

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
        public void Contains_ShouldReturnTrue()
        {
            // Arrange
            var toAdd = new List<RuleBasedSegment>
            {
                new RuleBasedSegment { Name = "segment1" },
                new RuleBasedSegment { Name = "segment2" }
            };

            _segmentCache.Update(toAdd, new List<string>(), 10);

            // Act & Assert
            Assert.IsTrue(_segmentCache.Contains(new List<string> { "segment1" }));
            Assert.IsFalse(_segmentCache.Contains(new List<string> { "segment1", "segment3" }));
            Assert.IsTrue(_segmentCache.Contains(new List<string> { "segment1", "segment2" }));
        }
    }
}
