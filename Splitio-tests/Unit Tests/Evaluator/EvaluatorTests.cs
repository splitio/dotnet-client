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

namespace Splitio_Tests.Unit_Tests.Evaluator
{
    [TestClass]
    public class EvaluatorTests
    {
        private readonly Mock<ISplitter> _splitter;
        private readonly Mock<ISplitCache> _splitCache;

        private readonly IEvaluator _evaluator;

        public EvaluatorTests()
        {
            _splitter = new Mock<ISplitter>();
            _splitCache = new Mock<ISplitCache>();

            _evaluator = new Splitio.Services.Evaluator.Evaluator(_splitCache.Object, _splitter.Object);
        }

        #region EvaluateFeature
        [TestMethod]
        public void EvaluateFeature_WhenSplitNameDoesntExist_ReturnsControl()
        {
            // Arrange.
            var splitName = "always_on";
            var key = new Key("test", "test");
            ParsedSplit parsedSplit = null;

            _splitCache
                .Setup(mock => mock.GetSplit(splitName))
                .Returns(parsedSplit);

            // Act.
            var result = _evaluator.EvaluateFeature(key, splitName);

            // Assert.
            Assert.AreEqual("control", result.Treatment);
            Assert.AreEqual(Labels.SplitNotFound, result.Label);
        }

        [TestMethod]
        public void EvaluateFeature_WhenSplitIsKilled_ReturnsDefaultTreatment()
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
                .Setup(mock => mock.GetSplit(splitName))
                .Returns(parsedSplit);

            // Act.
            var result = _evaluator.EvaluateFeature(key, splitName);

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, result.Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
            Assert.AreEqual(Labels.Killed, result.Label);
        }

        [TestMethod]
        public void EvaluateFeature_WhenSplitWithoutConditions_ReturnsDefaultTreatment()
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
                .Setup(mock => mock.GetSplit(splitName))
                .Returns(parsedSplit);

            // Act.
            var result = _evaluator.EvaluateFeature(key, splitName);

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, result.Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
            Assert.AreEqual(Labels.DefaultRule, result.Label);
        }

        [TestMethod]
        public void EvaluateFeature_WithRolloutCondition_BucketIsBiggerTrafficAllocation_ReturnsDefailtTreatment()
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
                        ConditionType = ConditionType.ROLLOUT,
                        Partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        Matcher = new CombiningMatcher
                        {
                            Combiner = CombinerEnum.AND,
                            Delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher()
                            }
                        }
                    }
                }
            };

            _splitCache
                .Setup(mock => mock.GetSplit(splitName))
                .Returns(parsedSplit);

            _splitter
                .Setup(mock => mock.GetBucket(key.bucketingKey, parsedSplit.trafficAllocationSeed, AlgorithmEnum.Murmur))
                .Returns(18);

            // Act.
            var result = _evaluator.EvaluateFeature(key, splitName);

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, result.Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
            Assert.AreEqual(Labels.TrafficAllocationFailed, result.Label);
        }

        [TestMethod]
        public void EvaluateFeature_WithRolloutCondition_TrafficAllocationIsBiggerBucket_ReturnsOn()
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
                        Label = "labelCondition",
                        ConditionType = ConditionType.ROLLOUT,
                        Partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        Matcher = new CombiningMatcher
                        {
                            Combiner = CombinerEnum.AND,
                            Delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    Matcher = new EndsWithMatcher(new List<string> { "@split.io" })
                                }
                            }
                        }
                    }
                }
            };

            _splitCache.Setup(mock => mock.GetSplit(splitName)).Returns(parsedSplit);

            _splitter
                .Setup(mock => mock.GetBucket(key.bucketingKey, parsedSplit.trafficAllocationSeed, AlgorithmEnum.Murmur))
                .Returns(18);

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var result = _evaluator.EvaluateFeature(key, splitName);

            // Assert.
            Assert.AreEqual("on", result.Treatment);
            Assert.AreEqual("labelCondition", result.Label);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
        }

        [TestMethod]
        public void EvaluateFeature_WithWhitelistCondition_EqualToBooleanMatcher_ReturnsOn()
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
                        Label = "label",
                        ConditionType = ConditionType.WHITELIST,
                        Partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        Matcher = new CombiningMatcher
                        {
                            Combiner = CombinerEnum.AND,
                            Delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    Matcher = new EqualToBooleanMatcher(true),
                                    Attribute = "true"
                                }
                            }                            
                        }
                    }
                }
            };

            _splitCache
                .Setup(mock => mock.GetSplit(splitName))
                .Returns(parsedSplit);

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var result = _evaluator.EvaluateFeature(key, splitName, attributes);

            // Assert.
            Assert.AreEqual("on", result.Treatment);
            Assert.AreEqual("label", result.Label);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
        }

        [TestMethod]
        public void EvaluateFeature_WithWhitelistCondition_EqualToBooleanMatcher_ReturnsOff()
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
                        Label = "label",
                        ConditionType = ConditionType.WHITELIST,
                        Partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        Matcher = new CombiningMatcher
                        {
                            Combiner = CombinerEnum.AND,
                            Delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    Matcher = new EqualToBooleanMatcher(false)
                                }
                            }
                        }
                    }
                }
            };

            _splitCache
                .Setup(mock => mock.GetSplit(splitName))
                .Returns(parsedSplit);

            // Act.
            var result = _evaluator.EvaluateFeature(key, splitName);

            // Assert.
            Assert.AreEqual("off", result.Treatment);
            Assert.AreEqual(Labels.DefaultRule, result.Label);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
        }

        [TestMethod]
        public void EvaluateFeature_WithTwoConditions_EndsWithMatch_ReturnsOn()
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
                        Label = "label",
                        ConditionType = ConditionType.WHITELIST,
                        Partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        Matcher = new CombiningMatcher
                        {
                            Combiner = CombinerEnum.AND,
                            Delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    Matcher = new EqualToBooleanMatcher(false)
                                }
                            }
                        }
                    },
                    new ConditionWithLogic
                    {
                        Label = "labelEndsWith",
                        ConditionType = ConditionType.WHITELIST,
                        Partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        Matcher = new CombiningMatcher
                        {
                            Combiner = CombinerEnum.AND,
                            Delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    Matcher = new EndsWithMatcher(new List<string> { "@split.io" })
                                }
                            }
                        }
                    },
                }
            };

            _splitCache
                .Setup(mock => mock.GetSplit(splitName))
                .Returns(parsedSplit);

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var result = _evaluator.EvaluateFeature(key, splitName);

            // Assert.
            Assert.AreEqual("on", result.Treatment);
            Assert.AreEqual("labelEndsWith", result.Label);
            Assert.AreEqual(parsedSplit.changeNumber, result.ChangeNumber);
        }
        #endregion

        #region EvaluateFeatures
        [TestMethod]
        public void EvaluateFeatures_WhenSplitNameDoesntExist_ReturnsControl()
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
                        Label = "labelEndsWithMatcher",
                        ConditionType = ConditionType.ROLLOUT,
                        Partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "on",
                               size = 100
                           }
                        },
                        Matcher = new CombiningMatcher
                        {
                            Combiner = CombinerEnum.AND,
                            Delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    Matcher = new EndsWithMatcher(new List<string> { "@split.io" })
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
                        Label = "labelWhiteList",
                        ConditionType = ConditionType.WHITELIST,
                        Partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "off",
                               size = 100
                           }
                        },
                        Matcher = new CombiningMatcher
                        {
                            Combiner = CombinerEnum.AND,
                            Delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    Matcher = new EndsWithMatcher(new List<string> { "@split.io" })
                                }
                            }
                        }
                    },
                    new ConditionWithLogic
                    {
                        Label = "labelRollout",
                        ConditionType = ConditionType.ROLLOUT,
                        Partitions = new List<PartitionDefinition>
                        {
                           new PartitionDefinition
                           {
                               treatment = "off",
                               size = 100
                           }
                        },
                        Matcher = new CombiningMatcher
                        {
                            Combiner = CombinerEnum.AND,
                            Delegates = new List<AttributeMatcher>
                            {
                                new AttributeMatcher
                                {
                                    Matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 123)
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
                .Setup(mock => mock.FetchMany(It.IsAny<List<string>>()))
                .Returns(new List<ParsedSplit>
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
            var result = _evaluator.EvaluateFeatures(key, splitNames);

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