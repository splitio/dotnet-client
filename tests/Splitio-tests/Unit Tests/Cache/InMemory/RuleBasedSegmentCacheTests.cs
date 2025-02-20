using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Cache.InMemory
{
    [TestClass]
    public class RuleBasedSegmentCacheTests
    {
        public RuleBasedSegmentCacheTests()
        {

        }

        [TestMethod]
        public void Update()
        {
            // Arrange.
            var cache = new InMemoryRuleBasedSegmentCache(new ConcurrentDictionary<string, RuleBasedSegment>());
            var toAdd = new List<RuleBasedSegment>
            {
                new RuleBasedSegment
                {
                    Name = "rbs-1",
                    ChangeNumber = 1,
                    CombiningMatchers = new List<CombiningMatcher>()
                },
                new RuleBasedSegment
                {
                    Name = "rbs-2",
                    ChangeNumber = 2,
                    CombiningMatchers = new List<CombiningMatcher>()
                }
            };

            // Act.
            cache.Update(toAdd, new List<string>(), 2);

            // Assert.
        }
    }
}
