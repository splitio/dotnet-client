using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing;
using Splitio.Services.SegmentFetcher.Interfaces;
using System;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Parsing
{
    [TestClass]
    public class RuleBasedSegmentParserTest
    {
        private Mock<ISegmentCacheConsumer> _segmentCacheMock;
        private Mock<ISegmentFetcher> _segmentFetcherMock;
        private Mock<IRuleBasedSegmentCacheConsumer> _rbsConsumer;
        private RuleBasedSegmentParser _parser;

        [TestInitialize]
        public void Setup()
        {
            _segmentCacheMock = new Mock<ISegmentCacheConsumer>();
            _segmentFetcherMock = new Mock<ISegmentFetcher>();
            _rbsConsumer = new Mock<IRuleBasedSegmentCacheConsumer>();

            _parser = new RuleBasedSegmentParser(_segmentCacheMock.Object, _segmentFetcherMock.Object);
        }

        [TestMethod]
        public void Parse_ValidDTO_ReturnsRuleBasedSegment()
        {
            // Arrange
            var rbsDTO = new RuleBasedSegmentDto
            {
                Status = "ACTIVE",
                Name = "test-segment",
                ChangeNumber = 123,
                Excluded = new Excluded{
                    Keys = new List<string> { "user1", "user2" },
                    Segments = null
                },
                Conditions = new List<ConditionDefinition>
                {
                    new ConditionDefinition
                    {
                        matcherGroup = new MatcherGroupDefinition
                        {
                            matchers = new List<MatcherDefinition>
                            {
                                new MatcherDefinition 
                                { 
                                    matcherType = "EQUAL_TO", unaryNumericMatcherData = new UnaryNumericData
                                    {
                                        dataType = DataTypeEnum.NUMBER,
                                        value = 123
                                    }
                                }
                            },
                            combiner = "AND"
                        }
                    }
                }
            };

            // Act
            var result = _parser.Parse(rbsDTO, _rbsConsumer.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test-segment", result.Name);
            Assert.AreEqual(123, result.ChangeNumber);
            CollectionAssert.AreEqual(new List<string> { "user1", "user2" }, result.Excluded.Keys);
            Assert.AreEqual(1, result.CombiningMatchers.Count);
        }

        [TestMethod]
        public void Parse_ValidDTO_ExcludedNull()
        {
            // Arrange
            var rbsDTO = new RuleBasedSegmentDto
            {
                Status = "ACTIVE",
                Name = "test-segment",
                ChangeNumber = 123,
                Excluded = null,
                Conditions = new List<ConditionDefinition>
                {
                    new ConditionDefinition
                    {
                        matcherGroup = new MatcherGroupDefinition
                        {
                            matchers = new List<MatcherDefinition>
                            {
                                new MatcherDefinition
                                {
                                    matcherType = "EQUAL_TO", unaryNumericMatcherData = new UnaryNumericData
                                    {
                                        dataType = DataTypeEnum.NUMBER,
                                        value = 123
                                    }
                                }
                            },
                            combiner = "AND"
                        }
                    }
                }
            };

            // Act
            var result = _parser.Parse(rbsDTO, _rbsConsumer.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Excluded);
            Assert.IsNotNull(result.Excluded.Keys);
            Assert.AreEqual(0, result.Excluded.Keys.Count);
            Assert.IsNotNull(result.Excluded.Segments);
            Assert.AreEqual(0, result.Excluded.Segments.Count);
        }

        [TestMethod]
        public void Parse_ValidDTO_ExcludedKeysNull()
        {
            // Arrange
            var rbsDTO = new RuleBasedSegmentDto
            {
                Status = "ACTIVE",
                Name = "test-segment",
                ChangeNumber = 123,
                Excluded = new Excluded
                {
                    Keys = null,
                    Segments = new List<ExcludedSegments>
                    {
                        new ExcludedSegments{ Type = "standard", Name = "seg1" },
                        new ExcludedSegments{ Type = "rule-based", Name = "seg2" },
                    }
                },
                Conditions = new List<ConditionDefinition>
                {
                    new ConditionDefinition
                    {
                        matcherGroup = new MatcherGroupDefinition
                        {
                            matchers = new List<MatcherDefinition>
                            {
                                new MatcherDefinition
                                {
                                    matcherType = "EQUAL_TO", unaryNumericMatcherData = new UnaryNumericData
                                    {
                                        dataType = DataTypeEnum.NUMBER,
                                        value = 123
                                    }
                                }
                            },
                            combiner = "AND"
                        }
                    }
                }
            };

            // Act
            var result = _parser.Parse(rbsDTO, _rbsConsumer.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Excluded.Keys.Count);
            Assert.AreEqual(2, result.Excluded.Segments.Count);
        }

        [TestMethod]
        public void Parse_InvalidStatus_ReturnsNull()
        {
            // Arrange
            var rbsDTO = new RuleBasedSegmentDto
            {
                Status = "ARCHIVE",
                Name = "test-segment",
                ChangeNumber = 123,
                Excluded = new Excluded{
                    Keys = new List<string> { "user1", "user2" }
                },
                Conditions = new List<ConditionDefinition>()
            };

            // Act
            var result = _parser.Parse(rbsDTO, _rbsConsumer.Object);

            // Assert
            Assert.IsNull(result);
        }
    }
}