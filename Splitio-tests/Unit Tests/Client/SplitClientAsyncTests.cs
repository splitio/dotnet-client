﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Client.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Evaluator;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Client
{
    [TestClass]
    public class SplitClientAsyncTests
    {
        private readonly Mock<IEventsLog> _eventsLog;
        private readonly Mock<IFeatureFlagCache> _splitCache;
        private readonly Mock<IImpressionsLog> _impressionsLog;
        private readonly Mock<IBlockUntilReadyService> _blockUntilReadyService;
        private readonly Mock<IEvaluator> _evaluator;
        private readonly Mock<IImpressionsManager> _impressionsManager;
        private readonly Mock<ISyncManager> _syncManager;

        private readonly ISplitClient _splitClient;
        public SplitClientAsyncTests()
        {
            _eventsLog = new Mock<IEventsLog>();
            _splitCache = new Mock<IFeatureFlagCache>();
            _impressionsLog = new Mock<IImpressionsLog>();
            _blockUntilReadyService = new Mock<IBlockUntilReadyService>();
            _evaluator = new Mock<IEvaluator>();
            _impressionsManager = new Mock<IImpressionsManager>();
            _syncManager = new Mock<ISyncManager>();

            _splitClient = new SplitClientForTesting(_splitCache.Object, _eventsLog.Object, _impressionsLog.Object, _blockUntilReadyService.Object, _evaluator.Object, _impressionsManager.Object, _syncManager.Object);
        }

        #region GetTreatmentAsync
        [TestMethod]
        public async Task GetTreatment_WithNullKey_ReturnsControl()
        {
            // Arrange 
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = await _splitClient.GetTreatmentAsync(null, string.Empty);

            // Assert
            Assert.AreEqual("control", result);
        }

        [TestMethod]
        public async Task GetTreatment_WhenNameDoesntExist_ReturnsControl()
        {
            // Arrange 
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluator
                .Setup(mock => mock.EvaluateFeatureAsync(It.IsAny<Key>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new TreatmentResult(Labels.SplitNotFound, "control", null));

            // Act
            var result = await _splitClient.GetTreatmentAsync("key", "not_exist");

            // Assert
            Assert.AreEqual("control", result);

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Once);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTreatment_WhenExist_ReturnsOn()
        {
            // Arrange 
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluator
                .Setup(mock => mock.EvaluateFeatureAsync(It.IsAny<Key>(), "feature_flag_test", It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new TreatmentResult(Labels.DefaultRule, "on", 1000, null, 11111));

            _impressionsManager
                .Setup(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()))
                .Returns(new KeyImpression());

            // Act
            var result = await _splitClient.GetTreatmentAsync("key", "feature_flag_test");

            // Assert
            Assert.AreEqual("on", result);

            _splitClient.Destroy();

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Once);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Once);
        }
        #endregion

        #region GetTreatmentsAsync
        [TestMethod]
        public async Task GetTreatments_WithEmptyKey_ShouldReturnControl()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var results = await _splitClient.GetTreatmentsAsync(string.Empty, new List<string> { string.Empty });

            // Assert
            foreach (var res in results)
            {
                Assert.AreEqual("control", res.Value);
            }

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Never);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTreatments_ShouldReturnTreatments()
        {
            // Arrange
            var treatmenOn = "always_on";
            var treatmenOff = "always_off";
            var configurations = new Dictionary<string, string>
            {
                { "off", "{\"name\": \"off config\", \"lastName\": \"split\"}" },
                { "on", "{\"name\": \"mauro\"}" }
            };
            var configExpectedOn = configurations["on"];
            var configExpectedOff = configurations["off"];

            var offConditions = new List<ConditionWithLogic>
            {
                new  ConditionWithLogic
                {
                    conditionType = ConditionType.ROLLOUT,
                    label = "default rule",
                    partitions = new List<PartitionDefinition>
                    {
                        new PartitionDefinition
                        {
                            size = 100,
                            treatment = "off"
                        },
                        new PartitionDefinition
                        {
                            size = 0,
                            treatment = "on"
                        }
                    },
                    matcher = new CombiningMatcher()
                }
            };

            var parsedSplitOn = SplitClientUnitTests.GetParsedSplit(treatmenOn, defaultTreatment: "off", configurations: configurations);
            var parsedSplitOff = SplitClientUnitTests.GetParsedSplit(treatmenOff, defaultTreatment: "on", configurations: configurations, conditions: offConditions, seed: 2095087413);

            _splitCache
                .Setup(mock => mock.GetSplitAsync(treatmenOn))
                .ReturnsAsync(parsedSplitOn);

            _splitCache
                .Setup(mock => mock.GetSplitAsync(treatmenOff))
                .ReturnsAsync(parsedSplitOff);

            _evaluator
                .SetupSequence(mock => mock.EvaluateFeaturesAsync(It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new MultipleEvaluatorResult
                {
                    TreatmentResults = new Dictionary<string, TreatmentResult>
                    {
                        { treatmenOff, new TreatmentResult("label", "off", null, configExpectedOff) },
                        { treatmenOn, new TreatmentResult("label", "on", null, configExpectedOn)}
                    }
                });

            _impressionsManager
                .Setup(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()))
                .Returns(new KeyImpression());

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = await _splitClient.GetTreatmentsAsync("user", new List<string> { treatmenOff, treatmenOn });

            // Assert
            var resultOn = result[parsedSplitOn.name];
            Assert.AreEqual("on", resultOn);

            var resultOff = result[parsedSplitOff.name];
            Assert.AreEqual("off", resultOff);

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Exactly(2));
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Once);
        }

        [TestMethod]
        public async Task GetTreatments_WhenNameDoesntExist_ReturnsControl()
        {
            // Arrange 
            var splitNames = new List<string> { "not_exist", "not_exist2" };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluator
                .SetupSequence(mock => mock.EvaluateFeaturesAsync(It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new MultipleEvaluatorResult
                {
                    TreatmentResults = new Dictionary<string, TreatmentResult>
                    {
                        { "control_treatment", new TreatmentResult(Labels.SplitNotFound, "control", null)}
                    }
                });

            // Act
            var result = await _splitClient.GetTreatmentsWithConfigAsync("key", splitNames);

            // Assert
            foreach (var res in result)
            {
                Assert.AreEqual("control", res.Value.Treatment);
                Assert.IsNull(res.Value.Config);
            }

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Once);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Never);
        }
        #endregion

        #region GetTreatmentWithConfigAsync
        [TestMethod]
        public async Task GetTreatmentWithConfig_WithEmptyKey_ShouldReturnControl()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = await _splitClient.GetTreatmentWithConfigAsync(string.Empty, string.Empty);

            // Assert
            Assert.AreEqual("control", result.Treatment);
            Assert.IsNull(result.Config);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfig_ShouldReturnOnWithConfig()
        {
            // Arrange
            var feature = "always_on";
            var treatmentExpected = "on";
            var configurations = new Dictionary<string, string>
            {
                { "off", "{\"name\": \"off config\", \"lastName\": \"split\"}" },
                { "on", "{\"name\": \"mauro\"}" }
            };
            var configExpected = configurations[treatmentExpected];
            var parsedSplit = SplitClientUnitTests.GetParsedSplit(feature, defaultTreatment: "off", configurations: configurations);

            _splitCache
                .Setup(mock => mock.GetSplitAsync(feature))
                .ReturnsAsync(parsedSplit);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluator
                .Setup(mock => mock.EvaluateFeatureAsync(It.IsAny<Key>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new TreatmentResult(Labels.DefaultRule, treatmentExpected, null, configExpected));

            _impressionsManager
                .Setup(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()))
                .Returns(new KeyImpression());

            // Act
            var result = await _splitClient.GetTreatmentWithConfigAsync("user", feature);

            // Assert
            Assert.AreEqual(treatmentExpected, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Once);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Once);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfig_WhenNotMach_ShouldReturnDefaultTreatmentWithConfig()
        {
            // Arrange
            var feature = "always_on";
            var defaultTreatment = "off";
            var configurations = new Dictionary<string, string>
            {
                { "off", "{\"name\": \"off config\", \"lastName\": \"split\"}" },
                { "on", "{\"name\": \"mauro\"}" }
            };
            var configExpected = configurations[defaultTreatment];

            var parsedSplit = SplitClientUnitTests.GetParsedSplit(feature, defaultTreatment, configurations: configurations);

            _splitCache
                .Setup(mock => mock.GetSplitAsync(feature))
                .ReturnsAsync(parsedSplit);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluator
                .Setup(mock => mock.EvaluateFeatureAsync(It.IsAny<Key>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new TreatmentResult("label", defaultTreatment, null, configExpected));

            _impressionsManager
                .Setup(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()))
                .Returns(new KeyImpression());

            // Act
            var result = await _splitClient.GetTreatmentWithConfigAsync("user", feature);

            // Assert
            Assert.AreEqual(defaultTreatment, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Once);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Once);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfig_WhenConfigIsNull_ShouldReturnOn()
        {
            // Arrange
            var feature = "always_on";
            var defaultTreatment = "off";
            var treatmentExpected = "on";

            var parsedSplit = SplitClientUnitTests.GetParsedSplit(feature, defaultTreatment);

            _splitCache
                .Setup(mock => mock.GetSplitAsync(feature))
                .ReturnsAsync(parsedSplit);

            _evaluator
                .Setup(mock => mock.EvaluateFeatureAsync(It.IsAny<Key>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new TreatmentResult("label", treatmentExpected, null));

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _impressionsManager
                .Setup(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()))
                .Returns(new KeyImpression());

            // Act
            var result = await _splitClient.GetTreatmentWithConfigAsync("user", feature);

            // Assert
            Assert.AreEqual(treatmentExpected, result.Treatment);
            Assert.IsNull(result.Config);

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Once);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Once);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfig_WhenIsKilled_ShouldReturnDefaultTreatmentWithConfig()
        {
            // Arrange
            var feature = "always_on";
            var defaultTreatment = "off";
            var configurations = new Dictionary<string, string>
            {
                { "off", "{\"name\": \"off config\", \"lastName\": \"split\"}" },
                { "on", "{\"name\": \"mauro\"}" }
            };
            var configExpected = configurations[defaultTreatment];

            var parsedSplit = SplitClientUnitTests.GetParsedSplit(feature, defaultTreatment, killed: true, configurations: configurations);

            _splitCache
                .Setup(mock => mock.GetSplitAsync(feature))
                .ReturnsAsync(parsedSplit);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluator
                .Setup(mock => mock.EvaluateFeatureAsync(It.IsAny<Key>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new TreatmentResult("label", defaultTreatment, null, configExpected));

            _impressionsManager
                .Setup(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()))
                .Returns(new KeyImpression());

            // Act
            var result = await _splitClient.GetTreatmentWithConfigAsync("user", feature);

            // Assert
            Assert.AreEqual(defaultTreatment, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Once);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Once);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfig_WhenTrafficAllocationIsSmallerThanBucket_ShouldReturnDefaultTreatmentWithConfig()
        {
            // Arrange
            var feature = "always_on";
            var defaultTreatment = "off";
            var configurations = new Dictionary<string, string>
            {
                { "off", "{\"name\": \"off config\", \"lastName\": \"split\"}" },
                { "on", "{\"name\": \"mauro\"}" }
            };
            var configExpected = configurations[defaultTreatment];

            var parsedSplit = SplitClientUnitTests.GetParsedSplit(feature, defaultTreatment, configurations: configurations, trafficAllocation: 20);

            _splitCache
                .Setup(mock => mock.GetSplitAsync(feature))
                .ReturnsAsync(parsedSplit);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluator
                .Setup(mock => mock.EvaluateFeatureAsync(It.IsAny<Key>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new TreatmentResult("label", defaultTreatment, config: configExpected));

            _impressionsManager
                .Setup(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()))
                .Returns(new KeyImpression());

            // Act
            var result = await _splitClient.GetTreatmentWithConfigAsync("user", feature);

            // Assert
            Assert.AreEqual(defaultTreatment, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Once);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Once);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfig_WhenConditionTypeIsWhitelist_ShouldReturnOntWithConfig()
        {
            // Arrange
            var feature = "always_on";
            var defaultTreatment = "off";
            var treatmentExpected = "on";
            var configurations = new Dictionary<string, string>
            {
                { "off", "{\"name\": \"off config\", \"lastName\": \"split\"}" },
                { "on", "{\"name\": \"mauro\"}" }
            };
            var configExpected = configurations[treatmentExpected];

            var conditions = new List<ConditionWithLogic>
            {
                new  ConditionWithLogic
                {
                    conditionType = ConditionType.WHITELIST,
                    label = "default rule",
                    partitions = new List<PartitionDefinition>
                    {
                        new PartitionDefinition
                        {
                            size = 100,
                            treatment = "on"
                        },
                        new PartitionDefinition
                        {
                            size = 0,
                            treatment = "off"
                        }
                    },
                    matcher = new CombiningMatcher()
                }
            };

            var parsedSplit = SplitClientUnitTests.GetParsedSplit(feature, defaultTreatment, configurations: configurations, trafficAllocation: 20, conditions: conditions);

            _splitCache
                .Setup(mock => mock.GetSplitAsync(feature))
                .ReturnsAsync(parsedSplit);

            _evaluator
                .Setup(mock => mock.EvaluateFeatureAsync(It.IsAny<Key>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new TreatmentResult("label", treatmentExpected, config: configExpected));

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = await _splitClient.GetTreatmentWithConfigAsync("user", feature);

            // Assert
            Assert.AreEqual(treatmentExpected, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfig_WhenConditionsIsEmpty_ShouldReturnDefaultTreatmenttWithConfig()
        {
            // Arrange
            var feature = "always_on";
            var defaultTreatment = "off";
            var configurations = new Dictionary<string, string>
            {
                { "off", "{\"name\": \"off config\", \"lastName\": \"split\"}" },
                { "on", "{\"name\": \"mauro\"}" }
            };
            var configExpected = configurations[defaultTreatment];
            var parsedSplit = SplitClientUnitTests.GetParsedSplit(feature, defaultTreatment, configurations: configurations, trafficAllocation: 20, conditions: new List<ConditionWithLogic>());

            _splitCache
                .Setup(mock => mock.GetSplitAsync(feature))
                .ReturnsAsync(parsedSplit);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluator
                .Setup(mock => mock.EvaluateFeatureAsync(It.IsAny<Key>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new TreatmentResult("label", defaultTreatment, config: configExpected));

            // Act
            var result = await _splitClient.GetTreatmentWithConfigAsync("user", feature);

            // Assert
            Assert.AreEqual(defaultTreatment, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);
        }

        [TestMethod]
        public async Task GetTreatmentWithConfig_WhenNameDoesntExist_ReturnsControl()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluator
                .Setup(mock => mock.EvaluateFeatureAsync(It.IsAny<Key>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new TreatmentResult(Labels.SplitNotFound, "control"));

            // Act
            var result = await _splitClient.GetTreatmentWithConfigAsync("key", "not_exist");

            // Assert
            Assert.AreEqual("control", result.Treatment);
            Assert.IsNull(result.Config);

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Once);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Never);
        }
        #endregion

        #region GetTreatmentsWithConfigAsync
        [TestMethod]
        public async Task GetTreatmentsWithConfig_WithEmptyKey_ShouldReturnControl()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var results = await _splitClient.GetTreatmentsWithConfigAsync(string.Empty, new List<string> { string.Empty });

            // Assert
            foreach (var res in results)
            {
                Assert.AreEqual("control", res.Value.Treatment);
                Assert.IsNull(res.Value.Config);
            }

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Never);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfig_ShouldReturnTreatmentsWithConfigs()
        {
            // Arrange
            var treatmenOn = "always_on";
            var treatmenOff = "always_off";
            var configurations = new Dictionary<string, string>
            {
                { "off", "{\"name\": \"off config\", \"lastName\": \"split\"}" },
                { "on", "{\"name\": \"mauro\"}" }
            };
            var configExpectedOn = configurations["on"];
            var configExpectedOff = configurations["off"];

            var offConditions = new List<ConditionWithLogic>
            {
                new  ConditionWithLogic
                {
                    conditionType = ConditionType.ROLLOUT,
                    label = "default rule",
                    partitions = new List<PartitionDefinition>
                    {
                        new PartitionDefinition
                        {
                            size = 100,
                            treatment = "off"
                        },
                        new PartitionDefinition
                        {
                            size = 0,
                            treatment = "on"
                        }
                    },
                    matcher = new CombiningMatcher()
                }
            };

            var parsedSplitOn = SplitClientUnitTests.GetParsedSplit(treatmenOn, defaultTreatment: "off", configurations: configurations);
            var parsedSplitOff = SplitClientUnitTests.GetParsedSplit(treatmenOff, defaultTreatment: "on", configurations: configurations, conditions: offConditions, seed: 2095087413);

            _splitCache
                .Setup(mock => mock.GetSplitAsync(treatmenOn))
                .ReturnsAsync(parsedSplitOn);

            _splitCache
                .Setup(mock => mock.GetSplitAsync(treatmenOff))
                .ReturnsAsync(parsedSplitOff);

            _evaluator
                .SetupSequence(mock => mock.EvaluateFeaturesAsync(It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new MultipleEvaluatorResult
                {
                    TreatmentResults = new Dictionary<string, TreatmentResult>
                    {
                        { treatmenOff, new TreatmentResult("label", "off", null, configExpectedOff) },
                        { treatmenOn, new TreatmentResult("label", "on", null, configExpectedOn)}
                    }
                });

            _impressionsManager
                .Setup(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()))
                .Returns(new KeyImpression());

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = await _splitClient.GetTreatmentsWithConfigAsync("user", new List<string> { treatmenOff, treatmenOn });

            // Assert
            var resultOn = result[parsedSplitOn.name];
            Assert.AreEqual("on", resultOn.Treatment);
            Assert.AreEqual(configExpectedOn, resultOn.Config);

            var resultOff = result[parsedSplitOff.name];
            Assert.AreEqual("off", resultOff.Treatment);
            Assert.AreEqual(configExpectedOff, resultOff.Config);

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Exactly(2));
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Once);
        }

        [TestMethod]
        public async Task GetTreatmentsWithConfig_WhenNameDoesntExist_ReturnsControl()
        {
            // Arrange 
            var splitNames = new List<string> { "not_exist", "not_exist2" };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluator
                .SetupSequence(mock => mock.EvaluateFeaturesAsync(It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(new MultipleEvaluatorResult
                {
                    TreatmentResults = new Dictionary<string, TreatmentResult>
                    {
                        { "control_treatment", new TreatmentResult(Labels.SplitNotFound, "control", null)}
                    }
                });

            // Act
            var result = await _splitClient.GetTreatmentsWithConfigAsync("key", splitNames);

            // Assert
            foreach (var res in result)
            {
                Assert.AreEqual("control", res.Value.Treatment);
                Assert.IsNull(res.Value.Config);
            }

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>(), It.IsAny<string>()), Times.Once);
            _impressionsManager.Verify(mock => mock.TrackAsync(It.IsAny<List<KeyImpression>>()), Times.Never);
        }
        #endregion
    }
}