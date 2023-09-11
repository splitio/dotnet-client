﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Evaluator
{
    [TestClass]
    public class EvaluatorAsyncTests
    {
        private readonly Mock<ISplitter> _splitter;
        private readonly Mock<IFeatureFlagCacheConsumer> _splitCache;

        private readonly IEvaluator _evaluator;

        public EvaluatorAsyncTests()
        {
            _splitter = new Mock<ISplitter>();
            _splitCache = new Mock<IFeatureFlagCacheConsumer>();

            _evaluator = new Splitio.Services.Evaluator.Evaluator(_splitCache.Object, _splitter.Object);
        }

        #region EvaluateFeature
        [TestMethod]
        public async Task EvaluateFeatureAsync_WhenSplitNameDoesntExist_ReturnsControl()
        {
            // Arrange.
            var splitName = "always_on";
            var key = new Key("test", "test");
            ParsedSplit parsedSplit = null;

            _splitCache
                .Setup(mock => mock.GetSplitAsync(splitName))
                .ReturnsAsync(parsedSplit);

            // Act.
            var result = await _evaluator.EvaluateFeatureAsync(key, splitName);

            // Assert.
            Assert.AreEqual("control", result.Treatment);
            Assert.AreEqual(Labels.SplitNotFound, result.Label);
        }

        [TestMethod]
        public async Task EvaluateFeatureAsync_WhenSplitIsKilled_ReturnsDefaultTreatment()
        {
            // Arrange.
            var splitName = "always_on";
            var key = new Key("test", "test");
            var parsedSplit = new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 123123,
                name = splitName,
                defaultTreatment = "off",
                trafficTypeName = "tt",
                killed = true
            };

            _splitCache
                .Setup(mock => mock.GetSplitAsync(splitName))
                .ReturnsAsync(parsedSplit);

            // Act.
            var result = await _evaluator.EvaluateFeatureAsync(key, splitName);

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, result.Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
            Assert.AreEqual(Labels.Killed, result.Label);
        }

        [TestMethod]
        public async Task EvaluateFeatureAsync_WhenSplitWithoutConditions_ReturnsDefaultTreatment()
        {
            // Arrange.
            var splitName = "always_on";
            var key = new Key("test", "test");
            var parsedSplit = new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 123123,
                name = splitName,
                defaultTreatment = "off",
                trafficTypeName = "tt",
                conditions = new List<ConditionWithLogic>()
            };

            _splitCache
                .Setup(mock => mock.GetSplitAsync(splitName))
                .ReturnsAsync(parsedSplit);

            // Act.
            var result = await _evaluator.EvaluateFeatureAsync(key, splitName);

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, result.Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
            Assert.AreEqual(Labels.DefaultRule, result.Label);
        }

        [TestMethod]
        public async Task EvaluateFeatureAsync_WithRolloutCondition_BucketIsBiggerTrafficAllocation_ReturnsDefailtTreatment()
        {
            // Arrange.
            var splitName = "always_on";
            var key = new Key("test", "test");
            var parsedSplit = new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 123123,
                name = splitName,
                defaultTreatment = "off",
                trafficTypeName = "tt",
                trafficAllocationSeed = 12,
                trafficAllocation = 10,
                conditions = new List<ConditionWithLogic>
                {
                    new ConditionWithLogic
                    {
                        conditionType = ConditionType.ROLLOUT,
                        partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        matcher = new CombiningMatcher
                        {
                            combiner = CombinerEnum.AND,
                            delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher()
                            }
                        }
                    }
                }
            };

            _splitCache
                .Setup(mock => mock.GetSplitAsync(splitName))
                .ReturnsAsync(parsedSplit);

            _splitter
                .Setup(mock => mock.GetBucket(key.bucketingKey, parsedSplit.trafficAllocationSeed, AlgorithmEnum.Murmur))
                .Returns(18);

            // Act.
            var result = await _evaluator.EvaluateFeatureAsync(key, splitName);

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, result.Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
            Assert.AreEqual(Labels.TrafficAllocationFailed, result.Label);
        }

        [TestMethod]
        public async Task EvaluateFeatureAsync_WithRolloutCondition_TrafficAllocationIsBiggerBucket_ReturnsOn()
        {
            // Arrange.
            var splitName = "always_on";
            var key = new Key("test@split.io", "test@split.io");
            var parsedSplit = new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 123123,
                name = splitName,
                defaultTreatment = "off",
                trafficTypeName = "tt",
                trafficAllocationSeed = 18,
                trafficAllocation = 20,
                conditions = new List<ConditionWithLogic>
                {
                    new ConditionWithLogic
                    {
                        label = "labelCondition",
                        conditionType = ConditionType.ROLLOUT,
                        partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        matcher = new CombiningMatcher
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
                }
            };

            _splitCache
                .Setup(mock => mock.GetSplitAsync(splitName))
                .ReturnsAsync(parsedSplit);

            _splitter
                .Setup(mock => mock.GetBucket(key.bucketingKey, parsedSplit.trafficAllocationSeed, AlgorithmEnum.Murmur))
                .Returns(18);

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var result = await _evaluator.EvaluateFeatureAsync(key, splitName);

            // Assert.
            Assert.AreEqual("on", result.Treatment);
            Assert.AreEqual("labelCondition", result.Label);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
        }

        [TestMethod]
        public async Task EvaluateFeatureAsync_WithWhitelistCondition_EqualToBooleanMatcher_ReturnsOn()
        {
            // Arrange.
            var splitName = "always_on";
            var key = new Key("true", "true");
            var attributes = new Dictionary<string, object> { { "true", true } };
            var parsedSplit = new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 123123,
                name = splitName,
                defaultTreatment = "off",
                trafficTypeName = "tt",
                trafficAllocationSeed = 18,
                trafficAllocation = 20,
                conditions = new List<ConditionWithLogic>
                {
                    new ConditionWithLogic
                    {
                        label = "label",
                        conditionType = ConditionType.WHITELIST,
                        partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        matcher = new CombiningMatcher
                        {
                            combiner = CombinerEnum.AND,
                            delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    matcher = new EqualToBooleanMatcher(true),
                                    attribute = "true"
                                }
                            }
                        }
                    }
                }
            };

            _splitCache
                .Setup(mock => mock.GetSplitAsync(splitName))
                .ReturnsAsync(parsedSplit);

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var result = await _evaluator.EvaluateFeatureAsync(key, splitName, attributes);

            // Assert.
            Assert.AreEqual("on", result.Treatment);
            Assert.AreEqual("label", result.Label);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
        }

        [TestMethod]
        public async Task EvaluateFeatureAsync_WithWhitelistCondition_EqualToBooleanMatcher_ReturnsOff()
        {
            // Arrange.
            var splitName = "always_on";
            var key = new Key("true", "true");
            var parsedSplit = new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 123123,
                name = splitName,
                defaultTreatment = "off",
                trafficTypeName = "tt",
                trafficAllocationSeed = 18,
                trafficAllocation = 20,
                conditions = new List<ConditionWithLogic>
                {
                    new ConditionWithLogic
                    {
                        label = "label",
                        conditionType = ConditionType.WHITELIST,
                        partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        matcher = new CombiningMatcher
                        {
                            combiner = CombinerEnum.AND,
                            delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    matcher = new EqualToBooleanMatcher(false)
                                }
                            }
                        }
                    }
                }
            };

            _splitCache
                .Setup(mock => mock.GetSplitAsync(splitName))
                .ReturnsAsync(parsedSplit);

            // Act.
            var result = await _evaluator.EvaluateFeatureAsync(key, splitName);

            // Assert.
            Assert.AreEqual("off", result.Treatment);
            Assert.AreEqual(Labels.DefaultRule, result.Label);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
        }

        [TestMethod]
        public async Task EvaluateFeatureAsync_WithTwoConditions_EndsWithMatch_ReturnsOn()
        {
            // Arrange.
            var splitName = "always_on";
            var key = new Key("mauro@split.io", "true");
            var parsedSplit = new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 123123,
                name = splitName,
                defaultTreatment = "off",
                trafficTypeName = "tt",
                trafficAllocationSeed = 18,
                trafficAllocation = 20,
                conditions = new List<ConditionWithLogic>
                {
                    new ConditionWithLogic
                    {
                        label = "label",
                        conditionType = ConditionType.WHITELIST,
                        partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        matcher = new CombiningMatcher
                        {
                            combiner = CombinerEnum.AND,
                            delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    matcher = new EqualToBooleanMatcher(false)
                                }
                            }
                        }
                    },
                    new ConditionWithLogic
                    {
                        label = "labelEndsWith",
                        conditionType = ConditionType.WHITELIST,
                        partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        matcher = new CombiningMatcher
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
                    },
                }
            };

            _splitCache
                .Setup(mock => mock.GetSplitAsync(splitName))
                .ReturnsAsync(parsedSplit);

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var result = await _evaluator.EvaluateFeatureAsync(key, splitName);

            // Assert.
            Assert.AreEqual("on", result.Treatment);
            Assert.AreEqual("labelEndsWith", result.Label);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
        }
        #endregion

        #region EvaluateFeatures
        [TestMethod]
        public async Task EvaluateFeaturesAsync_WhenSplitNameDoesntExist_ReturnsControl()
        {
            // Arrange.
            var splitNames = new List<string> { "always_on", "always_off" };
            var key = new Key("test@split.io", "test");
            var parsedSplitOn = new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 123123,
                name = splitNames.First(s => s.Equals("always_on")),
                defaultTreatment = "off",
                trafficTypeName = "tt",
                trafficAllocationSeed = 18,
                trafficAllocation = 20,
                seed = 123123133,
                conditions = new List<ConditionWithLogic>
                {
                    new ConditionWithLogic
                    {
                        label = "labelEndsWithMatcher",
                        conditionType = ConditionType.ROLLOUT,
                        partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        matcher = new CombiningMatcher
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
                }
            };

            var parsedSplitOff = new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 123123,
                name = splitNames.First(s => s.Equals("always_off")),
                defaultTreatment = "off",
                trafficTypeName = "tt",
                seed = 5647567,
                conditions = new List<ConditionWithLogic>
                {
                    new ConditionWithLogic
                    {
                        label = "labelWhiteList",
                        conditionType = ConditionType.WHITELIST,
                        partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "off",
                               size = 100
                           }
                        },
                        matcher = new CombiningMatcher
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
                    },
                    new ConditionWithLogic
                    {
                        label = "labelRollout",
                        conditionType = ConditionType.ROLLOUT,
                        partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "off",
                               size = 100
                           }
                        },
                        matcher = new CombiningMatcher
                        {
                            combiner = CombinerEnum.AND,
                            delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 123)
                                }
                            }
                        }
                    }
                }
            };

            _splitter
                .Setup(mock => mock.GetBucket(key.bucketingKey, parsedSplitOn.trafficAllocationSeed, AlgorithmEnum.Murmur))
                .Returns(18);

            _splitCache
                .Setup(mock => mock.FetchManyAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<ParsedSplit>
                {
                    parsedSplitOff,
                    parsedSplitOn
                });

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplitOn.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplitOn.algo))
                .Returns("on");

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplitOff.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplitOff.algo))
                .Returns("off");

            // Act.
            var result = await _evaluator.EvaluateFeaturesAsync(key, splitNames);

            // Assert.
            var resultOn = result.TreatmentResults.FirstOrDefault(tr => tr.Key.Equals("always_on"));
            Assert.AreEqual("on", resultOn.Value.Treatment);
            Assert.AreEqual(parsedSplitOn.changeNumber, resultOn.Value.ChangeNumber);
            Assert.AreEqual("labelEndsWithMatcher", resultOn.Value.Label);

            var resultOff = result.TreatmentResults.FirstOrDefault(tr => tr.Key.Equals("always_off"));
            Assert.AreEqual("off", resultOff.Value.Treatment);
            Assert.AreEqual(parsedSplitOn.changeNumber, resultOff.Value.ChangeNumber);
            Assert.AreEqual("labelWhiteList", resultOff.Value.Label);
        }
        #endregion
    }
}
