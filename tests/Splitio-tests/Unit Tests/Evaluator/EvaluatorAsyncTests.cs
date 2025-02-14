using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing;
using Splitio.Services.Parsing.Classes;
using Splitio.Telemetry.Storages;
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
        private readonly Mock<ITelemetryEvaluationProducer> _telemetryEvaluationProducer;

        private readonly IEvaluator _evaluator;

        public EvaluatorAsyncTests()
        {
            _splitter = new Mock<ISplitter>();
            _splitCache = new Mock<IFeatureFlagCacheConsumer>();
            _telemetryEvaluationProducer = new Mock<ITelemetryEvaluationProducer>();

            _evaluator = new Splitio.Services.Evaluator.Evaluator(_splitCache.Object, _splitter.Object, _telemetryEvaluationProducer.Object);
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
                .Setup(mock => mock.FetchManyAsync(new List<string> { splitName }))
                .ReturnsAsync(new List<ParsedSplit> { parsedSplit });

            // Act.
            var results = await _evaluator.EvaluateFeaturesAsync(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual("control", results.FirstOrDefault().TreatmentResult.Treatment);
            Assert.AreEqual(Labels.SplitNotFound, results.FirstOrDefault().TreatmentResult.Label);
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
                .Setup(mock => mock.FetchManyAsync(new List<string> { splitName }))
                .ReturnsAsync(new List<ParsedSplit> { parsedSplit });

            // Act.
            var results = await _evaluator.EvaluateFeaturesAsync(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, results.FirstOrDefault().TreatmentResult.Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().TreatmentResult.ChangeNumber);
            Assert.AreEqual(Labels.Killed, results.FirstOrDefault().TreatmentResult.Label);
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
                .Setup(mock => mock.FetchManyAsync(new List<string> { splitName }))
                .ReturnsAsync(new List<ParsedSplit> { parsedSplit });

            // Act.
            var results = await _evaluator.EvaluateFeaturesAsync(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, results.FirstOrDefault().TreatmentResult.Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().TreatmentResult.ChangeNumber);
            Assert.AreEqual(Labels.DefaultRule, results.FirstOrDefault().TreatmentResult.Label);
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
                .Setup(mock => mock.FetchManyAsync(new List<string> { splitName }))
                .ReturnsAsync(new List<ParsedSplit> { parsedSplit });

            _splitter
                .Setup(mock => mock.GetBucket(key.bucketingKey, parsedSplit.trafficAllocationSeed, AlgorithmEnum.Murmur))
                .Returns(18);

            // Act.
            var results = await _evaluator.EvaluateFeaturesAsync(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, results.FirstOrDefault().TreatmentResult.Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().TreatmentResult.ChangeNumber);
            Assert.AreEqual(Labels.TrafficAllocationFailed, results.FirstOrDefault().TreatmentResult.Label);
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
                .Setup(mock => mock.FetchManyAsync(new List<string> { splitName }))
                .ReturnsAsync(new List<ParsedSplit> { parsedSplit });

            _splitter
                .Setup(mock => mock.GetBucket(key.bucketingKey, parsedSplit.trafficAllocationSeed, AlgorithmEnum.Murmur))
                .Returns(18);

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var results = await _evaluator.EvaluateFeaturesAsync(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual("on", results.FirstOrDefault().TreatmentResult.Treatment);
            Assert.AreEqual("labelCondition", results.FirstOrDefault().TreatmentResult.Label);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().TreatmentResult.ChangeNumber);
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
                .Setup(mock => mock.FetchManyAsync(new List<string> { splitName }))
                .ReturnsAsync(new List<ParsedSplit> { parsedSplit });

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var results = await _evaluator.EvaluateFeaturesAsync(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName }, attributes);

            // Assert.
            Assert.AreEqual("on", results.FirstOrDefault().TreatmentResult.Treatment);
            Assert.AreEqual("label", results.FirstOrDefault().TreatmentResult.Label);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().TreatmentResult.ChangeNumber);
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
                .Setup(mock => mock.FetchManyAsync(new List<string> { splitName }))
                .ReturnsAsync(new List<ParsedSplit> { parsedSplit });

            // Act.
            var results = await _evaluator.EvaluateFeaturesAsync(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual("off", results.FirstOrDefault().TreatmentResult.Treatment);
            Assert.AreEqual(Labels.DefaultRule, results.FirstOrDefault().TreatmentResult.Label);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().TreatmentResult.ChangeNumber);
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
                .Setup(mock => mock.FetchManyAsync(new List<string> { splitName }))
                .ReturnsAsync(new List<ParsedSplit> { parsedSplit });

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var results = await _evaluator.EvaluateFeaturesAsync(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual("on", results.FirstOrDefault().TreatmentResult.Treatment);
            Assert.AreEqual("labelEndsWith", results.FirstOrDefault().TreatmentResult.Label);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().TreatmentResult.ChangeNumber);
        }
        #endregion

        #region EvaluateFeatures
        [TestMethod]
        public async Task EvaluateFeaturesAsync_WhenSplitNameDoesntExist_ReturnsControl()
        {
            // Arrange.
            var splitNames = new List<string> { "always_on", "always_off" };
            var key = new Key("test@split.io", "test");
            var parsedSplitOn = FeatureFlagOn();
            var parsedSplitOff = FeatureFlagOff();

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
            var results = await _evaluator.EvaluateFeaturesAsync(Splitio.Enums.API.GetTreatmentAsync, key, splitNames);

            // Assert.
            var resultOn = results.FirstOrDefault(tr => tr.TreatmentResult.FeatureFlagName.Equals("always_on"));
            Assert.AreEqual("on", resultOn.TreatmentResult.Treatment);
            Assert.AreEqual(parsedSplitOn.changeNumber, resultOn.TreatmentResult.ChangeNumber);
            Assert.AreEqual("labelEndsWithMatcher", resultOn.TreatmentResult.Label);

            var resultOff = results.FirstOrDefault(tr => tr.TreatmentResult.FeatureFlagName.Equals("always_off"));
            Assert.AreEqual("off", resultOff.TreatmentResult.Treatment);
            Assert.AreEqual(parsedSplitOn.changeNumber, resultOff.TreatmentResult.ChangeNumber);
            Assert.AreEqual("labelWhiteList", resultOff.TreatmentResult.Label);
        }
        #endregion

        #region EvaluateFeaturesByFlagSets
        [TestMethod]
        public async Task EvaluateFeaturesByFlagSetsAsync()
        {
            // Arrange.
            var key = new Key("test", "test");
            var sets = new List<string> { "set1", "set2", "set3" };

            _splitCache
                .Setup(mock => mock.GetNamesByFlagSetsAsync(sets))
                .ReturnsAsync(new Dictionary<string, HashSet<string>>
                {
                    { "set1", new HashSet<string> { "flag1", "flag2", "flag3" } },
                    { "set2", new HashSet<string> { "flag1", "flag2", "flag3" } },
                    { "set3", new HashSet<string>() }
                });

            _splitCache
                .Setup(mock => mock.FetchManyAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<ParsedSplit> { FeatureFlagOn("flag1"), FeatureFlagOff("flag2"), FeatureFlagOff("flag3") });

            // Act.
            var results = await _evaluator.EvaluateFeaturesByFlagSetsAsync(Splitio.Enums.API.GetTreatments, key, sets);

            // Assert.
            Assert.AreEqual(3, results.Count);
            var flag1 = results.First(x => x.TreatmentResult.FeatureFlagName.Equals("flag1"));
            Assert.AreEqual("on", flag1.TreatmentResult.Treatment);
            var flag2 = results.First(x => x.TreatmentResult.FeatureFlagName.Equals("flag2"));
            Assert.AreEqual("off", flag2.TreatmentResult.Treatment);
            var flag3 = results.First(x => x.TreatmentResult.FeatureFlagName.Equals("flag3"));
            Assert.AreEqual("off", flag3.TreatmentResult.Treatment);
        }
        #endregion

        #region Public Methods
        public static ParsedSplit FeatureFlagOff(string name = "always_off")
        {
            return new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 123123,
                name = name,
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
        }

        public static ParsedSplit FeatureFlagOn(string name = "always_on")
        {
            return  new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 123123,
                name = name,
                defaultTreatment = "on",
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
        }
        #endregion
    }
}
