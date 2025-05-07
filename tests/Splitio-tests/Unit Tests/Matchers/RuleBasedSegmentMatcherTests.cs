using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing;
using Splitio.Services.Parsing.Matchers;
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
        private readonly string _rbsName = "test-segment";

        [TestInitialize]
        public void Initialize()
        {
            _mockRuleBasedSegmentCache = new Mock<IRuleBasedSegmentCacheConsumer>();
            _mockSegmentsCache = new Mock<ISegmentCacheConsumer>();

            _matcher = new RuleBasedSegmentMatcher(_rbsName, _mockRuleBasedSegmentCache.Object, _mockSegmentsCache.Object);
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
                    Segments = new List<ExcludedSegments>
                    {
                        new ExcludedSegments
                        {
                            Type = "standard",
                            Name = "excluded-segment"
                        }
                    }
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

        [TestMethod]
        public void Match_WithKeyObject_ReturnsExpectedResult()
        {
            // Arrange
            var key = new Key("test-key", null);
            var rbs = new RuleBasedSegment
            {
                Excluded = new Excluded
                {
                    Keys = new List<string> { "excluded-key" },
                    Segments = new List<ExcludedSegments>
                    {
                        new ExcludedSegments
                        {
                            Type = "standard",
                            Name = "excluded-segment"
                        }
                    }
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
                .Setup(x => x.IsInSegment("excluded-segment", key.matchingKey))
                .Returns(false);

            // Act
            var result = _matcher.Match(key);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsync_WithKeyObject_ReturnsExpectedResult()
        {
            // Arrange
            var key = "test-key";
            var rbs = new RuleBasedSegment
            {
                Excluded = new Excluded
                {
                    Keys = new List<string> { "excluded-key" },
                    Segments = new List<ExcludedSegments>
                    {
                        new ExcludedSegments
                        {
                            Type = "standard",
                            Name = "excluded-segment"
                        }
                    }
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
                .Setup(x => x.GetAsync("test-segment"))
                .ReturnsAsync(rbs);

            _mockSegmentsCache
                .Setup(x => x.IsInSegmentAsync("excluded-segment", key))
                .ReturnsAsync(false);

            // Act
            var result = await _matcher.MatchAsync(key);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsync_WithStringKey_ReturnsExpectedResult()
        {
            // Arrange
            var key = new Key("test-key", null);
            var rbs = new RuleBasedSegment
            {
                Excluded = new Excluded
                {
                    Keys = new List<string> { "excluded-key" },
                    Segments = new List<ExcludedSegments>
                    {
                        new ExcludedSegments
                        {
                            Type = "standard",
                            Name = "excluded-segment"
                        }
                    }
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
                .Setup(x => x.GetAsync("test-segment"))
                .ReturnsAsync(rbs);

            _mockSegmentsCache
                .Setup(x => x.IsInSegmentAsync("excluded-segment", key.matchingKey))
                .ReturnsAsync(false);

            // Act
            var result = await _matcher.MatchAsync(key);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsync_ReturnsFalse_WhenSegmentNotFound()
        {
            // Arrange
            _mockRuleBasedSegmentCache
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((RuleBasedSegment)null);

            // Act
            var result = await _matcher.MatchAsync("test-key");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Match_ReturnsFalse_WhenSegmentNotFound()
        {
            // Arrange
            _mockRuleBasedSegmentCache
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns((RuleBasedSegment)null);

            // Act
            var result = _matcher.Match("test-key");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsync_ReturnsFalse_WhenKeyIsExcluded()
        {
            // Arrange
            var rbs = new RuleBasedSegment
            {
                Excluded = new Excluded
                {
                    Keys = new List<string> { "test-key" }
                }
            };
            
            _mockRuleBasedSegmentCache
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(rbs);

            // Act
            var result = await _matcher.MatchAsync(new Key("test-key", null));

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Match_ReturnsFalse_WhenKeyIsExcluded()
        {
            // Arrange
            var rbs = new RuleBasedSegment
            {
                Excluded = new Excluded
                {
                    Keys = new List<string> { "test-key" }
                }
            };

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns(rbs);

            // Act
            var result = _matcher.Match(new Key("test-key", null));

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsync_ReturnsFalse_WhenKeyIsExcludedBySegment()
        {
            // Arrange
            var rbs = new RuleBasedSegment
            {
                Excluded = new Excluded
                {
                    Keys = new List<string>(),
                    Segments = new List<ExcludedSegments>
                    {
                        new ExcludedSegments
                        {
                            Type = "standard",
                            Name = "test-key"
                        }
                    }
                },
                Name = "test-segment"
            };

            _mockRuleBasedSegmentCache
                .Setup(x => x.GetAsync("test-segment"))
                .ReturnsAsync(rbs);

            _mockSegmentsCache
                .Setup(x => x.IsInSegmentAsync("test-key", "test-key"))
                .ReturnsAsync(true);

            // Act
            var result = await _matcher.MatchAsync(new Key("test-key", null));

            // Assert
            Assert.IsFalse(result);
            _mockSegmentsCache.Verify(x => x.IsInSegmentAsync("test-key", "test-key"), Times.Once);
        }

        [TestMethod]
        public void Match_ReturnsFalse_WhenKeyIsExcludedBySegment()
        {
            // Arrange
            var rbs = new RuleBasedSegment
            {
                Excluded = new Excluded
                {
                    Keys = new List<string>(),
                    Segments = new List<ExcludedSegments>
                    {
                        new ExcludedSegments
                        {
                            Type = "standard",
                            Name = "test-key"
                        }
                    }
                },
                Name = "test-segment"
            };

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get("test-segment"))
                .Returns(rbs);

            _mockSegmentsCache
                .Setup(x => x.IsInSegment("test-key", "test-key"))
                .Returns(true);

            // Act
            var result = _matcher.Match(new Key("test-key", null));

            // Assert
            Assert.IsFalse(result);
            _mockSegmentsCache.Verify(x => x.IsInSegment("test-key", "test-key"), Times.Once);
        }

        [TestMethod]
        public async Task MatchAsync_ReturnsTrue_WhenCombiningMatchersMatch()
        {
            // Arrange
            var rbs = new RuleBasedSegment
            {
                Name = "test1",
                Excluded = new Excluded 
                {
                    Keys = new List<string>(),
                    Segments = new List<ExcludedSegments>()
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
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(rbs);

            // Act
            var result = await _matcher.MatchAsync(new Key("test-key", null));

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Match_ReturnsTrue_WhenCombiningMatchersMatch()
        {
            // Arrange
            var rbs = new RuleBasedSegment
            {
                Name = "test1",
                Excluded = new Excluded
                {
                    Keys = new List<string>(),
                    Segments = new List<ExcludedSegments>()
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
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns(rbs);

            // Act
            var result = _matcher.Match(new Key("test-key", null));

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsync_ReturnsFalse_WhenNoCombiningMatchersMatch()
        {
            // Arrange
            var rbs = new RuleBasedSegment
            {
                Name = "test1",
                Excluded = new Excluded
                {
                    Keys = new List<string>(),
                    Segments = new List<ExcludedSegments>()
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
                                matcher = new EndsWithMatcher(new List<string> { "@split.io" })
                            }
                        }
                    }
                }
            };

            _mockRuleBasedSegmentCache
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(rbs);

            // Act
            var result = await _matcher.MatchAsync(new Key("test-key", null));

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Match_ReturnsFalse_WhenNoCombiningMatchersMatch()
        {
            // Arrange
            var rbs = new RuleBasedSegment
            {
                Name = "test1",
                Excluded = new Excluded
                {
                    Keys = new List<string>(),
                    Segments = new List<ExcludedSegments>()
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
                                matcher = new EndsWithMatcher(new List<string> { "@split.io" })
                            }
                        }
                    }
                }
            };

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get(It.IsAny<string>()))
                .Returns(rbs);

            // Act
            var result = _matcher.Match(new Key("test-key", null));

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Match_WithRbsExcluded_ShouldReturnFalse()
        {
            // Arrange
            var excludedName = "rule-based_segment";
            var excludedSegment = new RuleBasedSegment
            {
                Name = excludedName,
                Excluded = new Excluded
                {
                    Keys = new List<string> { "test-key" }
                }
            };

            var rbs = new RuleBasedSegment
            {
                Name = _rbsName,
                Excluded = new Excluded
                {
                    Keys = new List<string>(),
                    Segments = new List<ExcludedSegments>
                    {
                        new ExcludedSegments { Type = "rule-based", Name = excludedName }
                    }
                },
                CombiningMatchers = new List<CombiningMatcher>()
            };

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get(_rbsName))
                .Returns(rbs);

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get(excludedName))
                .Returns(excludedSegment);

            // Act
            var result = _matcher.Match(new Key("test-key", null));

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Match_WithRbsExcluded_ShouldReturnTrue()
        {
            // Arrange
            var excludedName = "rule-based_segment";
            var excludedSegment = new RuleBasedSegment
            {
                Name = excludedName,
                Excluded = new Excluded
                {
                    Keys = new List<string> { "test-key-2" }
                },
                CombiningMatchers = new List<CombiningMatcher>()
            };

            var rbs = new RuleBasedSegment
            {
                Name = _rbsName,
                Excluded = new Excluded
                {
                    Keys = new List<string>(),
                    Segments = new List<ExcludedSegments>
                    {
                        new ExcludedSegments { Type = "rule-based", Name = excludedName }
                    }
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
                                matcher = new EndsWithMatcher(new List<string> { "@split.io" })
                            }
                        }
                    }
                }
            };

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get(_rbsName))
                .Returns(rbs);

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get(excludedName))
                .Returns(excludedSegment);

            // Act
            var result = _matcher.Match(new Key("mauro@split.io", null));

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsync_WithRbsExcluded_ShouldReturnFalse()
        {
            // Arrange
            var excludedName = "rule-based_segment";
            var excludedSegment = new RuleBasedSegment
            {
                Name = excludedName,
                Excluded = new Excluded
                {
                    Keys = new List<string> { "test-key" }
                }
            };

            var rbs = new RuleBasedSegment
            {
                Name = _rbsName,
                Excluded = new Excluded
                {
                    Keys = new List<string>(),
                    Segments = new List<ExcludedSegments>
                    {
                        new ExcludedSegments { Type = "rule-based", Name = excludedName }
                    }
                },
                CombiningMatchers = new List<CombiningMatcher>()
            };

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get(_rbsName))
                .Returns(rbs);

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get(excludedName))
                .Returns(excludedSegment);

            // Act
            var result = await _matcher.MatchAsync(new Key("test-key", null));

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsync_WithRbsExcluded_ShouldReturnTrue()
        {
            // Arrange
            var excludedName = "rule-based_segment";
            var excludedSegment = new RuleBasedSegment
            {
                Name = excludedName,
                Excluded = new Excluded
                {
                    Keys = new List<string> { "test-key-2" }
                },
                CombiningMatchers = new List<CombiningMatcher>()
            };

            var rbs = new RuleBasedSegment
            {
                Name = _rbsName,
                Excluded = new Excluded
                {
                    Keys = new List<string>(),
                    Segments = new List<ExcludedSegments>
                    {
                        new ExcludedSegments { Type = "rule-based", Name = excludedName }
                    }
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
                                matcher = new EndsWithMatcher(new List<string> { "@split.io" })
                            }
                        }
                    }
                }
            };

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get(_rbsName))
                .Returns(rbs);

            _mockRuleBasedSegmentCache
                .Setup(x => x.Get(excludedName))
                .Returns(excludedSegment);

            // Act
            var result = await _matcher.MatchAsync(new Key("mauro@split.io", null));

            // Assert
            Assert.IsTrue(result);
        }
    }
}
