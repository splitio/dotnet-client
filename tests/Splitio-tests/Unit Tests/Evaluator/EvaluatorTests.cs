using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.EngineEvaluator;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing;
using Splitio.Services.Parsing.Classes;
using Splitio.Services.Parsing.Matchers;
using Splitio.Telemetry.Storages;
using System.Collections.Generic;
using System.Linq;

namespace Splitio_Tests.Unit_Tests.Evaluator
{
    [TestClass]
    public class EvaluatorTests
    {
        private readonly Mock<ISplitter> _splitter;
        private readonly Mock<IFeatureFlagCache> _splitCache;
        private readonly Mock<ITelemetryEvaluationProducer> _telemetryEvaluationProducer;

        private readonly IEvaluator _evaluator;

        public EvaluatorTests()
        {
            _splitter = new Mock<ISplitter>();
            _splitCache = new Mock<IFeatureFlagCache>();
            _telemetryEvaluationProducer = new Mock<ITelemetryEvaluationProducer>();

            _evaluator = new Splitio.Services.Evaluator.Evaluator(_splitCache.Object, _splitter.Object, _telemetryEvaluationProducer.Object);
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
                .Setup(mock => mock.FetchMany(new List<string> { splitName }))
                .Returns(new List<ParsedSplit> { parsedSplit });

            // Act.
            var results = _evaluator.EvaluateFeatures(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual("control", results.FirstOrDefault().Treatment);
            Assert.AreEqual(Labels.SplitNotFound, results.FirstOrDefault().Label);
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
                .Setup(mock => mock.FetchMany(new List<string> { splitName }))
                .Returns(new List<ParsedSplit> { parsedSplit });

            // Act.
            var results = _evaluator.EvaluateFeatures(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, results.FirstOrDefault().Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().ChangeNumber);
            Assert.AreEqual(Labels.Killed, results.FirstOrDefault().Label);
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
                .Setup(mock => mock.FetchMany(new List<string> { splitName }))
                .Returns(new List<ParsedSplit> { parsedSplit });

            // Act.
            var results = _evaluator.EvaluateFeatures(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, results.FirstOrDefault().Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().ChangeNumber);
            Assert.AreEqual(Labels.DefaultRule, results.FirstOrDefault().Label);
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
                .Setup(mock => mock.FetchMany(new List<string> { splitName }))
                .Returns(new List<ParsedSplit> { parsedSplit });

            _splitter
                .Setup(mock => mock.GetBucket(key.bucketingKey, parsedSplit.trafficAllocationSeed, AlgorithmEnum.Murmur))
                .Returns(18);

            // Act.
            var results = _evaluator.EvaluateFeatures(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual(parsedSplit.defaultTreatment, results.FirstOrDefault().Treatment);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().ChangeNumber);
            Assert.AreEqual(Labels.TrafficAllocationFailed, results.FirstOrDefault().Label);
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
                .Setup(mock => mock.FetchMany(new List<string> { splitName }))
                .Returns(new List<ParsedSplit> { parsedSplit });

            _splitter
                .Setup(mock => mock.GetBucket(key.bucketingKey, parsedSplit.trafficAllocationSeed, AlgorithmEnum.Murmur))
                .Returns(18);

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var results = _evaluator.EvaluateFeatures(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual("on", results.FirstOrDefault().Treatment);
            Assert.AreEqual("labelCondition", results.FirstOrDefault().Label);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().ChangeNumber);
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
                .Setup(mock => mock.FetchMany(new List<string> { splitName }))
                .Returns(new List<ParsedSplit> { parsedSplit });

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var results = _evaluator.EvaluateFeatures(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName }, attributes);

            // Assert.
            Assert.AreEqual("on", results.FirstOrDefault().Treatment);
            Assert.AreEqual("label", results.FirstOrDefault().Label);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().ChangeNumber);
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
                .Setup(mock => mock.FetchMany(new List<string> { splitName }))
                .Returns(new List<ParsedSplit> { parsedSplit });

            // Act.
            var results = _evaluator.EvaluateFeatures(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual("off", results.FirstOrDefault().Treatment);
            Assert.AreEqual(Labels.DefaultRule, results.FirstOrDefault().Label);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().ChangeNumber);
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
                .Setup(mock => mock.FetchMany(new List<string> { splitName }))
                .Returns(new List<ParsedSplit> { parsedSplit });

            _splitter
                .Setup(mock => mock.GetTreatment(key.bucketingKey, parsedSplit.seed, It.IsAny<List<PartitionDefinition>>(), parsedSplit.algo))
                .Returns("on");

            // Act.
            var results = _evaluator.EvaluateFeatures(Splitio.Enums.API.GetTreatmentAsync, key, new List<string> { splitName });

            // Assert.
            Assert.AreEqual("on", results.FirstOrDefault().Treatment);
            Assert.AreEqual("labelEndsWith", results.FirstOrDefault().Label);
            Assert.AreEqual(parsedSplit.changeNumber, results.FirstOrDefault().ChangeNumber);
        }

        [TestMethod]
        public void EvaluateFeatures_WithPrerequisites_ReturnsTreatment()
        {
            // Arrange
            var ffName1 = "featureFlagTest";
            var ffName2 = "ffPrerequisites";
            var flag = new ParsedSplit
            {
                name = ffName1,
                Prerequisites = new PrerequisitesMatcher(new List<PrerequisitesDto>
                {
                    new PrerequisitesDto
                    {
                        FeatureFlagName = ffName2,
                        Treatments = new List<string>{ "v1" }
                    }
                }),
                conditions = new List<ConditionWithLogic>
                {
                    new ConditionWithLogic
                    {
                        conditionType = ConditionType.ROLLOUT,
                        label = "label",
                        partitions = new List<PartitionDefinition>
                        {
                            new PartitionDefinition
                            {
                                size = 100,
                                treatment = "testOk"
                            }
                        },
                        matcher = new CombiningMatcher
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
                }
            };

            _splitCache
                .Setup(mock => mock.FetchMany(new List<string> { ffName1 }))
                .Returns(new List<ParsedSplit> { flag });
            
            _splitCache
                .Setup(mock => mock.FetchMany(new List<string> { ffName2 }))
                .Returns(new List<ParsedSplit> { new ParsedSplit
                {
                    name = ffName2,
                    conditions = new List<ConditionWithLogic>
                    {
                        new ConditionWithLogic
                        {
                            conditionType = ConditionType.ROLLOUT,
                            label = "label",
                            matcher = new CombiningMatcher
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
                    }
                }});

            _splitter
                .SetupSequence(mock => mock.GetTreatment(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<PartitionDefinition>>(), It.IsAny<AlgorithmEnum>()))
                .Returns("v1")
                .Returns("testOk");

            // Act
            var results = _evaluator.EvaluateFeatures(Splitio.Enums.API.GetTreatment, new Key("matching-key", null), new List<string> { ffName1 });

            // Assert
            var res = results.FirstOrDefault();
            Assert.AreEqual("testOk", res.Treatment);
            Assert.AreEqual(ffName1, res.FeatureFlagName);
        }

        [TestMethod]
        public void EvaluateFeatures_WithPrerequisites_ReturnsDefaultTreatment()
        {
            // Arrange
            var ffName1 = "featureFlagTest";
            var ffName2 = "ffPrerequisites";
            var flag = new ParsedSplit
            {
                name = ffName1,
                defaultTreatment = "defaultTreatment",
                Prerequisites = new PrerequisitesMatcher(new List<PrerequisitesDto>
                {
                    new PrerequisitesDto
                    {
                        FeatureFlagName = ffName2,
                        Treatments = new List<string>{ "v1" }
                    }
                }),
                conditions = new List<ConditionWithLogic>
                {
                    new ConditionWithLogic
                    {
                        conditionType = ConditionType.ROLLOUT,
                        label = "label",
                        partitions = new List<PartitionDefinition>
                        {
                            new PartitionDefinition
                            {
                                size = 100,
                                treatment = "testOk"
                            }
                        },
                        matcher = new CombiningMatcher
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
                }
            };

            _splitCache
                .Setup(mock => mock.FetchMany(new List<string> { ffName1 }))
                .Returns(new List<ParsedSplit> { flag });

            _splitCache
                .Setup(mock => mock.FetchMany(new List<string> { ffName2 }))
                .Returns(new List<ParsedSplit> { new ParsedSplit
                {
                    name = ffName2,
                    conditions = new List<ConditionWithLogic>
                    {
                        new ConditionWithLogic
                        {
                            conditionType = ConditionType.ROLLOUT,
                            label = "label",
                            matcher = new CombiningMatcher
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
                    }
                }});

            _splitter
                .Setup(mock => mock.GetTreatment(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<PartitionDefinition>>(), It.IsAny<AlgorithmEnum>()))
                .Returns("v2");

            // Act
            var results = _evaluator.EvaluateFeatures(Splitio.Enums.API.GetTreatment, new Key("matching-key", null), new List<string> { ffName1 });

            // Assert
            var res = results.FirstOrDefault();
            Assert.AreEqual(flag.defaultTreatment, res.Treatment);
            Assert.AreEqual(ffName1, res.FeatureFlagName);
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
            var results = _evaluator.EvaluateFeatures(Splitio.Enums.API.GetTreatmentAsync, key, splitNames);

            // Assert.
            var resultOn = results.FirstOrDefault(tr => tr.FeatureFlagName.Equals("always_on"));
            Assert.AreEqual("on", resultOn.Treatment);
            Assert.AreEqual(parsedSplitOn.changeNumber, resultOn.ChangeNumber);
            Assert.AreEqual("labelEndsWithMatcher", resultOn.Label);

            var resultOff = results.FirstOrDefault(tr => tr.FeatureFlagName.Equals("always_off"));
            Assert.AreEqual("off", resultOff.Treatment);
            Assert.AreEqual(parsedSplitOn.changeNumber, resultOff.ChangeNumber);
            Assert.AreEqual("labelWhiteList", resultOff.Label);
        }
        #endregion

        #region EvaluateFeaturesByFlagSets
        [TestMethod]
        public void EvaluateFeaturesByFlagSets()
        {
            // Arrange.
            var key = new Key("test", "test");
            var sets = new List<string> { "set1", "set2", "set3" };

            _splitCache
                .Setup(mock => mock.GetNamesByFlagSets(sets))
                .Returns(new Dictionary<string, HashSet<string>>
                {
                    { "set1", new HashSet<string> { "flag1", "flag2", "flag3" } },
                    { "set2", new HashSet<string> { "flag1", "flag2", "flag3" } },
                    { "set3", new HashSet<string>() }
                });

            _splitCache
                .Setup(mock => mock.FetchMany(It.IsAny<List<string>>()))
                .Returns(new List<ParsedSplit> { EvaluatorAsyncTests.FeatureFlagOn("flag1"), EvaluatorAsyncTests.FeatureFlagOff("flag2"), EvaluatorAsyncTests.FeatureFlagOff("flag3") });

            // Act.
            var results = _evaluator.EvaluateFeaturesByFlagSets(Splitio.Enums.API.GetTreatments, key, sets);

            // Assert.
            Assert.AreEqual(3, results.Count);
            var flag1 = results.First(x => x.FeatureFlagName.Equals("flag1"));
            Assert.AreEqual("on", flag1.Treatment);
            var flag2 = results.First(x => x.FeatureFlagName.Equals("flag2"));
            Assert.AreEqual("off", flag2.Treatment);
            var flag3 = results.First(x => x.FeatureFlagName.Equals("flag3"));
            Assert.AreEqual("off", flag3.Treatment);
        }
        #endregion
    }
}