using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing;
using Splitio.Services.Parsing.Matchers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class RuleBasedSegmentMatcherTest
    {
        private Mock<IRuleBasedSegmentCacheConsumer> _mockRuleBasedSegmentCache;
        private Mock<ISegmentCacheConsumer> _mockSegmentsCache;
        private RuleBasedSegmentMatcher _matcher;

        [TestInitialize]
        public void Initialize()
        {
            _mockRuleBasedSegmentCache = new Mock<IRuleBasedSegmentCacheConsumer>();
            _mockSegmentsCache = new Mock<ISegmentCacheConsumer>();

            _matcher = new RuleBasedSegmentMatcher("test-segment", _mockRuleBasedSegmentCache.Object, _mockSegmentsCache.Object);
        }

        [TestMethod]
        public void Match_WithStringKey_ReturnsExpectedResult()
        {
            // Arrange
            var key = "test-key";
            var rbs = new RuleBasedSegment
            {
                Excluded = new Excluded
                {
                    Keys = new List<string> { "excluded-key" },
                    Segments = new List<string> { "excluded-segment" }
                },
                CombiningMatchers = new List<CombiningMatcher>
                {
                    new CombiningMatcher
                    {
                        combiner = CombinerEnum.AND,
                        delegates = new List<AttributeMatcher>
                        {
                            new AttributeMatcher
                            {
                                matcher = new AllKeysMatcher()
                            }
                        }
                    }
                }
            };

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get("test-segment"))
                .Returns(rbs);
            
            _mockSegmentsCache
                .Setup(x => x.IsInSegment("excluded-segment", key))
                .Returns(false);

            // Act
            var result = _matcher.Match(key);

            // Assert
            Assert.IsTrue(result);
        }

        //[TestMethod]
        //public void Match_WithKeyObject_ReturnsExpectedResult()
        //{
        //    // Arrange
        //    var key = new Key("test-key", null);
        //    var rbs = new RuleBasedSegment
        //    {
        //        Excluded = new Excluded
        //        {
        //            Keys = new List<string> { "excluded-key" },
        //            Segments = new List<string> { "excluded-segment" }
        //        },
        //        CombiningMatchers = new List<BaseMatcher> { new AlwaysTrueMatcher() }
        //    };
        //    _mockRuleBasedSegmentCache.Setup(x => x.Get("test-segment")).Returns(rbs);
        //    _mockSegmentsCache.Setup(x => x.IsInSegment("excluded-segment", key.matchingKey)).Returns(false);

        //    // Act
        //    var result = _matcher.Match(key);

        //    // Assert
        //    Assert.IsTrue(result);
        //}

        //[TestMethod]
        //public async Task MatchAsync_WithKeyObject_ReturnsExpectedResult()
        //{
        //    // Arrange
        //    var key = new Key("test-key", null);
        //    var rbs = new RuleBasedSegment
        //    {
        //        Excluded = new ExcludedSegment
        //        {
        //            Keys = new HashSet<string> { "excluded-key" },
        //            Segments = new List<string> { "excluded-segment" }
        //        },
        //        CombiningMatchers = new List<BaseMatcher> { new AlwaysTrueMatcher() }
        //    };
        //    _mockRuleBasedSegmentCache.Setup(x => x.GetAsync("test-segment")).ReturnsAsync(rbs);
        //    _mockSegmentsCache.Setup(x => x.IsInSegment("excluded-segment", key.matchingKey)).Returns(false);

        //    // Act
        //    var result = await _matcher.MatchAsync(key);

        //    // Assert
        //    Assert.IsTrue(result);
        //}

        //[TestMethod]
        //public async Task MatchAsync_WithStringKey_ReturnsExpectedResult()
        //{
        //    // Arrange
        //    var key = "test-key";
        //    var rbs = new RuleBasedSegment
        //    {
        //        Excluded = new ExcludedSegment
        //        {
        //            Keys = new HashSet<string> { "excluded-key" },
        //            Segments = new List<string> { "excluded-segment" }
        //        },
        //        CombiningMatchers = new List<BaseMatcher> { new AlwaysTrueMatcher() }
        //    };
        //    _mockRuleBasedSegmentCache.Setup(x => x.GetAsync("test-segment")).ReturnsAsync(rbs);
        //    _mockSegmentsCache.Setup(x => x.IsInSegment("excluded-segment", key)).Returns(false);

        //    // Act
        //    var result = await _matcher.MatchAsync(key);

        //    // Assert
        //    Assert.IsTrue(result);
        //}
    }
}
