using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Constants;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;
using System.Linq;

namespace Splitio_Tests.Unit_Tests
{
    [TestClass]
    public class SplitParserUnitTests
    {
        [TestMethod]
        public void ParseSuccessfullyWhenNonSpecifiedAlgorithm()
        {
            //Arrange
            Split split = new Split
            {
                name = "test1",
                seed = 2323,
                status = "ACTIVE",
                killed = false,
                defaultTreatment = "off",
                changeNumber = 232323,
                trafficTypeName = "user",
                conditions = new List<ConditionDefinition>()
            };

            var parser = new InMemorySplitParser(null, null);

            //Act
            var parsedSplit = parser.Parse(split);

            //Assert
            Assert.IsNotNull(parsedSplit);
            Assert.AreEqual(split.name, parsedSplit.name);
            Assert.AreEqual(split.seed, parsedSplit.seed);
            Assert.AreEqual(split.killed, parsedSplit.killed);
            Assert.AreEqual(split.defaultTreatment, parsedSplit.defaultTreatment);
            Assert.AreEqual(split.changeNumber, parsedSplit.changeNumber);
            Assert.AreEqual(AlgorithmEnum.LegacyHash, parsedSplit.algo);
            Assert.AreEqual(split.trafficTypeName, parsedSplit.trafficTypeName);
        }

        [TestMethod]
        public void ParseSuccessfullyWhenLegacyAlgorithm()
        {
            //Arrange
            Split split = new Split
            {
                name = "test1",
                seed = 2323,
                status = "ACTIVE",
                killed = false,
                defaultTreatment = "off",
                changeNumber = 232323,
                algo = 1,
                trafficTypeName = "user",
                conditions = new List<ConditionDefinition>()
            };

            var parser = new InMemorySplitParser(null, null);

            //Act
            var parsedSplit = parser.Parse(split);

            //Assert
            Assert.IsNotNull(parsedSplit);
            Assert.AreEqual(split.name, parsedSplit.name);
            Assert.AreEqual(split.seed, parsedSplit.seed);
            Assert.AreEqual(split.killed, parsedSplit.killed);
            Assert.AreEqual(split.defaultTreatment, parsedSplit.defaultTreatment);
            Assert.AreEqual(split.changeNumber, parsedSplit.changeNumber);
            Assert.AreEqual(AlgorithmEnum.LegacyHash, parsedSplit.algo);
            Assert.AreEqual(split.trafficTypeName, parsedSplit.trafficTypeName);
        }

        [TestMethod]
        public void ParseSuccessfullyWhenMurmurAlgorithm()
        {
            //Arrange
            Split split = new Split
            {
                name = "test1",
                seed = 2323,
                status = "ACTIVE",
                killed = false,
                defaultTreatment = "off",
                changeNumber = 232323,
                algo = 2,
                trafficTypeName = "user",
                conditions = new List<ConditionDefinition>()
            };

            var parser = new InMemorySplitParser(null, null);

            //Act
            var parsedSplit = parser.Parse(split);

            //Assert
            Assert.IsNotNull(parsedSplit);
            Assert.AreEqual(split.name, parsedSplit.name);
            Assert.AreEqual(split.seed, parsedSplit.seed);
            Assert.AreEqual(split.killed, parsedSplit.killed);
            Assert.AreEqual(split.defaultTreatment, parsedSplit.defaultTreatment);
            Assert.AreEqual(split.changeNumber, parsedSplit.changeNumber);
            Assert.AreEqual(AlgorithmEnum.Murmur, parsedSplit.algo);
            Assert.AreEqual(split.trafficTypeName, parsedSplit.trafficTypeName);
        }

        [TestMethod]
        public void ParseWithUnssupportedMatcher()
        {
            // Arrange.
            var segmentCacheConsumer = new Mock<ISegmentCacheConsumer>();
            var parser = new SplitParser(segmentCacheConsumer.Object);

            var split = new Split
            {
                name = "test1",
                seed = 2323,
                status = "ACTIVE",
                killed = false,
                defaultTreatment = "off",
                changeNumber = 232323,
                trafficTypeName = "user",
                conditions = new List<ConditionDefinition>
                {
                    new ConditionDefinition
                    {
                        matcherGroup = new MatcherGroupDefinition
                        {
                            matchers = new List<MatcherDefinition>
                            {
                                new MatcherDefinition
                                {
                                    matcherType = "NEW_MATCHER_TYPE"
                                }
                            }
                        },
                    },
                    new ConditionDefinition
                    {
                        matcherGroup = new MatcherGroupDefinition
                        {
                            matchers = new List<MatcherDefinition>
                            {
                                new MatcherDefinition
                                {
                                    matcherType = "ALL_KEYS"
                                }
                            }
                        },
                    },
                }
            };

            // Act.
            var result = parser.Parse(split);

            // Assert.
            Assert.AreEqual("test1", result.name);
            Assert.AreEqual("off", result.defaultTreatment);
            Assert.AreEqual(1, result.conditions.Count);
            var condition = result.conditions[0];
            Assert.AreEqual(Labels.UnsupportedMatcherType, condition.label);
            Assert.AreEqual(ConditionType.WHITELIST, condition.conditionType);
            Assert.AreEqual(1, condition.partitions.Count);
            var partition = condition.partitions[0];
            Assert.AreEqual(Gral.Control, partition.treatment);
            Assert.AreEqual(100, partition.size);
        }

        [TestMethod]
        public void ParseWithEqualToSemver()
        {
            // Arrange.
            var segmentCacheConsumer = new Mock<ISegmentCacheConsumer>();
            var parser = new SplitParser(segmentCacheConsumer.Object);

            var split = new Split
            {
                name = "test1",
                seed = 2323,
                status = "ACTIVE",
                killed = false,
                defaultTreatment = "off",
                changeNumber = 232323,
                trafficTypeName = "user",
                conditions = new List<ConditionDefinition>
                {
                    new ConditionDefinition
                    {
                        conditionType = "ROLLOUT",
                        label = "new label",
                        partitions = new List<PartitionDefinition>
                        {
                            new PartitionDefinition
                            {
                                size = 50,
                                treatment = "on"
                            },
                            new PartitionDefinition
                            {
                                size = 50,
                                treatment = "0ff"
                            }
                        },
                        matcherGroup = new MatcherGroupDefinition
                        {
                            matchers = new List<MatcherDefinition>
                            {
                                new MatcherDefinition
                                {
                                    matcherType = "EQUAL_TO_SEMVER",
                                    stringMatcherData = "2.2.2"
                                }
                            }
                        },
                    }
                }
            };

            // Act.
            var result = parser.Parse(split);

            // Assert.
            Assert.AreEqual("test1", result.name);
            Assert.AreEqual("off", result.defaultTreatment);
            Assert.AreEqual(1, result.conditions.Count);
            var condition = result.conditions[0];
            Assert.AreEqual("new label", condition.label);
            Assert.AreEqual(ConditionType.ROLLOUT, condition.conditionType);
            Assert.AreEqual(2, condition.partitions.Count);
            Assert.IsInstanceOfType(condition.matcher.delegates.FirstOrDefault().matcher, typeof(EqualToSemverMatcher));
        }

        [TestMethod]
        public void ParseWithGreaterThanOrEqualToSemver()
        {
            // Arrange.
            var segmentCacheConsumer = new Mock<ISegmentCacheConsumer>();
            var parser = new SplitParser(segmentCacheConsumer.Object);

            var split = new Split
            {
                name = "test1",
                seed = 2323,
                status = "ACTIVE",
                killed = false,
                defaultTreatment = "off",
                changeNumber = 232323,
                trafficTypeName = "user",
                conditions = new List<ConditionDefinition>
                {
                    new ConditionDefinition
                    {
                        conditionType = "ROLLOUT",
                        label = "new label",
                        partitions = new List<PartitionDefinition>
                        {
                            new PartitionDefinition
                            {
                                size = 50,
                                treatment = "on"
                            },
                            new PartitionDefinition
                            {
                                size = 50,
                                treatment = "0ff"
                            }
                        },
                        matcherGroup = new MatcherGroupDefinition
                        {
                            matchers = new List<MatcherDefinition>
                            {
                                new MatcherDefinition
                                {
                                    matcherType = "GREATER_THAN_OR_EQUAL_TO_SEMVER",
                                    stringMatcherData = "2.2.2"
                                }
                            }
                        },
                    }
                }
            };

            // Act.
            var result = parser.Parse(split);

            // Assert.
            Assert.AreEqual("test1", result.name);
            Assert.AreEqual("off", result.defaultTreatment);
            Assert.AreEqual(1, result.conditions.Count);
            var condition = result.conditions[0];
            Assert.AreEqual("new label", condition.label);
            Assert.AreEqual(ConditionType.ROLLOUT, condition.conditionType);
            Assert.AreEqual(2, condition.partitions.Count);
            Assert.IsInstanceOfType(condition.matcher.delegates.FirstOrDefault().matcher, typeof(GreaterThanOrEqualToSemverMatcher));
        }

        [TestMethod]
        public void ParseWithLessThanOrEqualToSemver()
        {
            // Arrange.
            var segmentCacheConsumer = new Mock<ISegmentCacheConsumer>();
            var parser = new SplitParser(segmentCacheConsumer.Object);

            var split = new Split
            {
                name = "test1",
                seed = 2323,
                status = "ACTIVE",
                killed = false,
                defaultTreatment = "off",
                changeNumber = 232323,
                trafficTypeName = "user",
                conditions = new List<ConditionDefinition>
                {
                    new ConditionDefinition
                    {
                        conditionType = "ROLLOUT",
                        label = "new label",
                        partitions = new List<PartitionDefinition>
                        {
                            new PartitionDefinition
                            {
                                size = 50,
                                treatment = "on"
                            },
                            new PartitionDefinition
                            {
                                size = 50,
                                treatment = "0ff"
                            }
                        },
                        matcherGroup = new MatcherGroupDefinition
                        {
                            matchers = new List<MatcherDefinition>
                            {
                                new MatcherDefinition
                                {
                                    matcherType = "LESS_THAN_OR_EQUAL_TO_SEMVER",
                                    stringMatcherData = "2.2.2"
                                }
                            }
                        },
                    }
                }
            };

            // Act.
            var result = parser.Parse(split);

            // Assert.
            Assert.AreEqual("test1", result.name);
            Assert.AreEqual("off", result.defaultTreatment);
            Assert.AreEqual(1, result.conditions.Count);
            var condition = result.conditions[0];
            Assert.AreEqual("new label", condition.label);
            Assert.AreEqual(ConditionType.ROLLOUT, condition.conditionType);
            Assert.AreEqual(2, condition.partitions.Count);
            Assert.IsInstanceOfType(condition.matcher.delegates.FirstOrDefault().matcher, typeof(LessThanOrEqualToSemverMatcher));
        }

        [TestMethod]
        public void ParseWithBetweenSemver()
        {
            // Arrange.
            var segmentCacheConsumer = new Mock<ISegmentCacheConsumer>();
            var parser = new SplitParser(segmentCacheConsumer.Object);

            var split = new Split
            {
                name = "test1",
                seed = 2323,
                status = "ACTIVE",
                killed = false,
                defaultTreatment = "off",
                changeNumber = 232323,
                trafficTypeName = "user",
                conditions = new List<ConditionDefinition>
                {
                    new ConditionDefinition
                    {
                        conditionType = "ROLLOUT",
                        label = "new label",
                        partitions = new List<PartitionDefinition>
                        {
                            new PartitionDefinition
                            {
                                size = 50,
                                treatment = "on"
                            },
                            new PartitionDefinition
                            {
                                size = 50,
                                treatment = "0ff"
                            }
                        },
                        matcherGroup = new MatcherGroupDefinition
                        {
                            matchers = new List<MatcherDefinition>
                            {
                                new MatcherDefinition
                                {
                                    matcherType = "BETWEEN_SEMVER",
                                    BetweenStringMatcherData = new BetweenStringData
                                    {
                                        start = "1.1.1",
                                        end = "3.3.3"
                                    }
                                }
                            }
                        },
                    }
                }
            };

            // Act.
            var result = parser.Parse(split);

            // Assert.
            Assert.AreEqual("test1", result.name);
            Assert.AreEqual("off", result.defaultTreatment);
            Assert.AreEqual(1, result.conditions.Count);
            var condition = result.conditions[0];
            Assert.AreEqual("new label", condition.label);
            Assert.AreEqual(ConditionType.ROLLOUT, condition.conditionType);
            Assert.AreEqual(2, condition.partitions.Count);
            Assert.IsInstanceOfType(condition.matcher.delegates.FirstOrDefault().matcher, typeof(BetweenSemverMatcher));
        }
    }
}
