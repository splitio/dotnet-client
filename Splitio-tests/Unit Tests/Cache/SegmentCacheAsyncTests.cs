using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public async Task IsInSegmentAsyncTestFalse()
        {
            //Arrange
            var segmentName = "segment_test";

            //Act
            var result = await _cache.IsInSegmentAsync(segmentName, "abcd");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsInSegmentAsyncTestTrue()
        {
            //Arrange
            var segmentName = "segment_test";

            _cache.AddToSegment(segmentName, new List<string> { "abcd", "zzzzf" });

            //Act
            var result = await _cache.IsInSegmentAsync(segmentName, "abcd");

            //Assert
            Assert.IsTrue(result);
        }
    }
}
