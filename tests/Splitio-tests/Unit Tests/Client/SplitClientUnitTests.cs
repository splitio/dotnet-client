﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Evaluator;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Client
{
    [TestClass]
    public class SplitClientUnitTests
    {
        private Mock<IEventsLog> _eventsLogMock;
        private Mock<IFeatureFlagCache> _splitCacheMock;
        private Mock<IBlockUntilReadyService> _blockUntilReadyService;
        private Mock<IEvaluator> _evaluatorMock;
        private Mock<IImpressionsManager> _impressionsManager;
        private Mock<ISyncManager> _syncManager;

        private SplitClientForTesting _splitClientForTesting;

        [TestInitialize]
        public void TestInitialize()
        {
            _splitCacheMock = new Mock<IFeatureFlagCache>();
            _eventsLogMock = new Mock<IEventsLog>();
            _blockUntilReadyService = new Mock<IBlockUntilReadyService>();
            _evaluatorMock = new Mock<IEvaluator>();
            _impressionsManager = new Mock<IImpressionsManager>();
            _syncManager = new Mock<ISyncManager>();

            _splitClientForTesting = new SplitClientForTesting(_splitCacheMock.Object, _eventsLogMock.Object, new Mock<IImpressionsLog>().Object, _blockUntilReadyService.Object, _evaluatorMock.Object, _impressionsManager.Object, _syncManager.Object);

            _splitClientForTesting.BlockUntilReady(1000);
        }

        #region GetTreatment
        [TestMethod]
        public void GetTreatment_ShouldReturnControl_WithNullKey()
        {
            // Arrange 
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = _splitClientForTesting.GetTreatment((string)null, string.Empty);

            // Assert
            Assert.AreEqual("control", result);
        }

        [TestMethod]
        public void GetTreatment_ShouldReturnControl_WithNullMatchingKey()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = _splitClientForTesting.GetTreatment(new Key(null, string.Empty), string.Empty);

            // Assert
            Assert.AreEqual("control", result);
        }

        [TestMethod]
        public void GetTreatment_ShouldReturnControl_WithNullMatchingAndBucketingKey()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = _splitClientForTesting.GetTreatment(new Key(null, null), string.Empty);

            // Assert
            Assert.AreEqual("control", result);
        }

        [TestMethod]
        public void GetTreatment_WhenNameDoesntExist_ReturnsControl()
        {
            // Arrange 
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluatorMock
                .Setup(mock => mock.EvaluateFeatures(It.IsAny<Splitio.Enums.API>(), It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), true))
                .Returns( new List<TreatmentResult> { new TreatmentResult("not_exist", Labels.SplitNotFound, "control", false) });

            // Act
            var result = _splitClientForTesting.GetTreatment("key", "not_exist");

            // Assert
            Assert.AreEqual("control", result);

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>()), Times.Once);
            _impressionsManager.Verify(mock => mock.Track(It.IsAny<List<KeyImpression>>()), Times.Never);
        }
        #endregion

        #region GetTreatmentWithConfig
        [TestMethod]
        public void GetTreatmentWithConfig_WithEmptyKey_ShouldReturnControl()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = _splitClientForTesting.GetTreatmentWithConfig(string.Empty, string.Empty);

            // Assert
            Assert.AreEqual("control", result.Treatment);
            Assert.IsNull(result.Config);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WithNullKey_ShouldReturnControl()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = _splitClientForTesting.GetTreatmentWithConfig((Key)null, string.Empty);

            // Assert
            Assert.AreEqual("control", result.Treatment);
            Assert.IsNull(result.Config);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_ShouldReturnOnWithConfig()
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
            var parsedSplit = GetParsedSplit(feature, defaultTreatment: "off", configurations: configurations);

            _splitCacheMock
                .Setup(mock => mock.GetSplit(feature))
                .Returns(parsedSplit);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluatorMock
                .Setup(mock => mock.EvaluateFeatures(It.IsAny<Splitio.Enums.API>(), It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), true))
                .Returns(new List<TreatmentResult> { new TreatmentResult(feature, "label", treatmentExpected, false, config: configExpected) });

            // Act
            var result = _splitClientForTesting.GetTreatmentWithConfig("user", feature);

            // Assert
            Assert.AreEqual(treatmentExpected, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WhenNotMach_ShouldReturnDefaultTreatmentWithConfig()
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

            var parsedSplit = GetParsedSplit(feature, defaultTreatment, configurations: configurations);

            _splitCacheMock
                .Setup(mock => mock.GetSplit(feature))
                .Returns(parsedSplit);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluatorMock
                .Setup(mock => mock.EvaluateFeatures(It.IsAny<Splitio.Enums.API>(), It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), true))
                .Returns(new List<TreatmentResult> { new TreatmentResult(feature, "label", defaultTreatment, false, config: configExpected) });

            // Act
            var result = _splitClientForTesting.GetTreatmentWithConfig("user", feature);

            // Assert
            Assert.AreEqual(defaultTreatment, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WhenConfigIsNull_ShouldReturnOn()
        {
            // Arrange
            var feature = "always_on";
            var defaultTreatment = "off";
            var treatmentExpected = "on";

            var parsedSplit = GetParsedSplit(feature, defaultTreatment);

            _splitCacheMock
                .Setup(mock => mock.GetSplit(feature))
                .Returns(parsedSplit);

            _evaluatorMock
                .Setup(mock => mock.EvaluateFeatures(It.IsAny<Splitio.Enums.API>(), It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), true))
                .Returns(new List<TreatmentResult> { new TreatmentResult(feature, "label", treatmentExpected, false) });

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = _splitClientForTesting.GetTreatmentWithConfig("user", feature);

            // Assert
            Assert.AreEqual(treatmentExpected, result.Treatment);
            Assert.IsNull(result.Config);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WhenIsKilled_ShouldReturnDefaultTreatmentWithConfig()
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

            var parsedSplit = GetParsedSplit(feature, defaultTreatment, killed: true, configurations: configurations);

            _splitCacheMock
                .Setup(mock => mock.GetSplit(feature))
                .Returns(parsedSplit);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluatorMock
                .Setup(mock => mock.EvaluateFeatures(It.IsAny<Splitio.Enums.API>(), It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), true))
                .Returns(new List<TreatmentResult> { new TreatmentResult(feature, "label", defaultTreatment, false, config: configExpected) });

            // Act
            var result = _splitClientForTesting.GetTreatmentWithConfig("user", feature);

            // Assert
            Assert.AreEqual(defaultTreatment, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WhenTrafficAllocationIsSmallerThanBucket_ShouldReturnDefaultTreatmentWithConfig()
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

            var parsedSplit = GetParsedSplit(feature, defaultTreatment, configurations: configurations, trafficAllocation: 20);

            _splitCacheMock
                .Setup(mock => mock.GetSplit(feature))
                .Returns(parsedSplit);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluatorMock
                .Setup(mock => mock.EvaluateFeatures(It.IsAny<Splitio.Enums.API>(), It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), true))
                .Returns(new List<TreatmentResult> { new TreatmentResult(feature, "label", defaultTreatment, false, config: configExpected) });

            // Act
            var result = _splitClientForTesting.GetTreatmentWithConfig("user", feature);

            // Assert
            Assert.AreEqual(defaultTreatment, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WhenConditionTypeIsWhitelist_ShouldReturnOntWithConfig()
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

            var parsedSplit = GetParsedSplit(feature, defaultTreatment, configurations: configurations, trafficAllocation: 20, conditions: conditions);

            _splitCacheMock
                .Setup(mock => mock.GetSplit(feature))
                .Returns(parsedSplit);

            _evaluatorMock
                .Setup(mock => mock.EvaluateFeatures(It.IsAny<Splitio.Enums.API>(), It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), true))
                .Returns(new List<TreatmentResult> { new TreatmentResult(feature, "label", treatmentExpected, false, config: configExpected) });

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = _splitClientForTesting.GetTreatmentWithConfig("user", feature);

            // Assert
            Assert.AreEqual(treatmentExpected, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WhenConditionsIsEmpty_ShouldReturnDefaultTreatmenttWithConfig()
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
            var parsedSplit = GetParsedSplit(feature, defaultTreatment, configurations: configurations, trafficAllocation: 20, conditions: new List<ConditionWithLogic>());

            _splitCacheMock
                .Setup(mock => mock.GetSplit(feature))
                .Returns(parsedSplit);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluatorMock
                .Setup(mock => mock.EvaluateFeatures(It.IsAny<Splitio.Enums.API>(), It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), true))
                .Returns(new List<TreatmentResult> { new TreatmentResult(feature, "label", defaultTreatment, false, config: configExpected) });

            // Act
            var result = _splitClientForTesting.GetTreatmentWithConfig("user", feature);

            // Assert
            Assert.AreEqual(defaultTreatment, result.Treatment);
            Assert.AreEqual(configExpected, result.Config);
        }

        [TestMethod]
        public void GetTreatmentWithConfig_WhenNameDoesntExist_ReturnsControl()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluatorMock
                .Setup(mock => mock.EvaluateFeatures(It.IsAny<Splitio.Enums.API>(), It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), true))
                .Returns(new List<TreatmentResult> { new TreatmentResult("not_exist", Labels.SplitNotFound, "control", false) });

            // Act
            var result = _splitClientForTesting.GetTreatmentWithConfig("key", "not_exist");

            // Assert
            Assert.AreEqual("control", result.Treatment);
            Assert.IsNull(result.Config);

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>()), Times.Once);
            _impressionsManager.Verify(mock => mock.Track(It.IsAny<List<KeyImpression>>()), Times.Never);
        }
        #endregion

        #region GetTreatmentsWithConfig
        [TestMethod]
        public void GetTreatmentsWithConfig_WithEmptyKey_ShouldReturnControl()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var results = _splitClientForTesting.GetTreatmentsWithConfig(string.Empty, new List<string> { string.Empty });

            // Assert
            foreach (var res in results)
            {
                Assert.AreEqual("control", res.Value.Treatment);
                Assert.IsNull(res.Value.Config);
            }
        }

        [TestMethod]
        public void GetTreatmentsWithConfig_WithNullKey_ShouldReturnControl()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var results = _splitClientForTesting.GetTreatmentsWithConfig((Key)null, new List<string> { string.Empty });

            // Assert
            foreach (var res in results)
            {
                Assert.AreEqual("control", res.Value.Treatment);
                Assert.IsNull(res.Value.Config);
            }
        }

        [TestMethod]
        public void GetTreatmentsWithConfig_ShouldReturnTreatmentsWithConfigs()
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

            var parsedSplitOn = GetParsedSplit(treatmenOn, defaultTreatment: "off", configurations: configurations);
            var parsedSplitOff = GetParsedSplit(treatmenOff, defaultTreatment: "on", configurations: configurations, conditions: offConditions, seed: 2095087413);

            _splitCacheMock
                .Setup(mock => mock.GetSplit(treatmenOn))
                .Returns(parsedSplitOn);

            _splitCacheMock
                .Setup(mock => mock.GetSplit(treatmenOff))
                .Returns(parsedSplitOff);

            _evaluatorMock
                .SetupSequence(mock => mock.EvaluateFeatures(It.IsAny<Splitio.Enums.API>(), It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), true))
                .Returns(new List<TreatmentResult>
                {
                    new TreatmentResult(treatmenOff, "label", "off", false, config: configExpectedOff),
                    new TreatmentResult(treatmenOn, "label", "on", false, config: configExpectedOn)
                });

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = _splitClientForTesting.GetTreatmentsWithConfig("user", new List<string> { treatmenOff, treatmenOn });

            // Assert
            var resultOn = result[parsedSplitOn.name];
            Assert.AreEqual("on", resultOn.Treatment);
            Assert.AreEqual(configExpectedOn, resultOn.Config);

            var resultOff = result[parsedSplitOff.name];
            Assert.AreEqual("off", resultOff.Treatment);
            Assert.AreEqual(configExpectedOff, resultOff.Config);
        }

        [TestMethod]
        public void GetTreatmentsWithConfig_WhenNameDoesntExist_ReturnsControl()
        {
            // Arrange 
            var splitNames = new List<string> { "not_exist", "not_exist2" };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _evaluatorMock
                .SetupSequence(mock => mock.EvaluateFeatures(It.IsAny<Splitio.Enums.API>(), It.IsAny<Key>(), It.IsAny<List<string>>(), It.IsAny<Dictionary<string, object>>(), true))
                .Returns(new List<TreatmentResult> { new TreatmentResult("control_treatment", Labels.SplitNotFound, "control", false) });

            // Act
            var result = _splitClientForTesting.GetTreatmentsWithConfig("key", splitNames);

            // Assert
            foreach (var res in result)
            {
                Assert.AreEqual("control", res.Value.Treatment);
                Assert.IsNull(res.Value.Config);
            }

            _impressionsManager.Verify(mock => mock.Build(It.IsAny<TreatmentResult>(), It.IsAny<Key>()), Times.Once);
            _impressionsManager.Verify(mock => mock.Track(It.IsAny<List<KeyImpression>>()), Times.Never);
        }
        #endregion

        #region Track
        [TestMethod]
        public void Track_ShouldReturnFalse_WithNullKey()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = _splitClientForTesting.Track(null, string.Empty, string.Empty);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Track_ShouldReturnFalse_WithNullTrafficType()
        {
            //Arrange 
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = _splitClientForTesting.Track(string.Empty, null, string.Empty);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Track_ShouldReturnFalse_WithNullEventType()
        {
            // Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act
            var result = _splitClientForTesting.Track(string.Empty, string.Empty, null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Track_WithProperties_ReturnsTrue()
        {
            // Arrange. 
            decimal decimalValue = 111;
            float floatValue = 112;
            double doubleValue = 113;
            short shortValue = 114;
            int intValue = 115;
            long longValue = 116;
            ushort ushortValue = 117;
            uint uintValue = 118;
            ulong ulongValue = 119;

            var properties = new Dictionary<string, object>
            {
                { "property_1", "value1" },
                { "property_2", new ParsedSplit() },
                { "property_3", false },
                { "property_4", null },
                { "property_5", decimalValue },
                { "property_6", floatValue },
                { "property_7", doubleValue },
                { "property_8", shortValue },
                { "property_9", intValue },
                { "property_10", longValue },
                { "property_11", ushortValue },
                { "property_12", uintValue },
                { "property_13", ulongValue }
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act.
            var result = _splitClientForTesting.Track("key", "user", "event_type", 132, properties);

            // Assert.
            Thread.Sleep(1500);
            Assert.IsTrue(result);
            _eventsLogMock.Verify(mock => mock.Log(It.Is<WrappedEvent>(we => we.Event.properties != null &&
                                                                             we.Event.key.Equals("key") &&
                                                                             we.Event.eventTypeId.Equals("event_type") &&
                                                                             we.Event.trafficTypeName.Equals("user") &&
                                                                             we.Event.value == 132)), Times.Once);
        }

        [TestMethod]
        public void Track_WhenPropertiesIsNull_ReturnsTrue()
        {
            // Arrange.
            Dictionary<string, object> properties = null;

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act.
            var result = _splitClientForTesting.Track("key", "user", "event_type", 132, properties);

            // Assert.
            Thread.Sleep(1500);
            Assert.IsTrue(result);
            _eventsLogMock.Verify(mock => mock.Log(It.Is<WrappedEvent>(we => we.Event.properties == null &&
                                                                             we.Event.key.Equals("key") &&
                                                                             we.Event.eventTypeId.Equals("event_type") &&
                                                                             we.Event.trafficTypeName.Equals("user") &&
                                                                             we.Event.value == 132)), Times.Once);
        }

        [TestMethod]
        public void Track_WhenTraffictTypeDoesNotExist_ReturnsTrue()
        {
            // Arrange.
            var trafficType = "traffict_type";

            _splitCacheMock
                .Setup(mock => mock.TrafficTypeExists(trafficType))
                .Returns(false);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act.
            var result = _splitClientForTesting.Track("key", trafficType, "event_type", 132);

            // Assert.
            Thread.Sleep(1500);
            Assert.IsTrue(result);
            _eventsLogMock.Verify(mock => mock.Log(It.Is<WrappedEvent>(we => we.Event.properties == null &&
                                                                             we.Event.key.Equals("key") &&
                                                                             we.Event.eventTypeId.Equals("event_type") &&
                                                                             we.Event.trafficTypeName.Equals(trafficType) &&
                                                                             we.Event.value == 132)), Times.Once);
        }

        [TestMethod]
        public void Track_WhenTraffictTypeExists_ReturnsTrue()
        {
            // Arrange.
            var trafficType = "traffict_type";

            _splitCacheMock
                .Setup(mock => mock.TrafficTypeExists(trafficType))
                .Returns(true);

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            // Act.
            var result = _splitClientForTesting.Track("key", trafficType, "event_type", 132);

            // Assert.
            Thread.Sleep(1500);
            Assert.IsTrue(result);
            _eventsLogMock.Verify(mock => mock.Log(It.Is<WrappedEvent>(we => we.Event.properties == null &&
                                                                             we.Event.key.Equals("key") &&
                                                                             we.Event.eventTypeId.Equals("event_type") &&
                                                                             we.Event.trafficTypeName.Equals(trafficType) &&
                                                                             we.Event.value == 132)), Times.Once);
        }
        #endregion

        #region Destroy
        [TestMethod]
        public void Destroy_ShouldsDecreaseFactoryInstatiation()
        {
            // Act
            _splitClientForTesting.Destroy();

            // Assert
            Assert.IsTrue(_splitClientForTesting.IsDestroyed());
        }
        #endregion

        public static ParsedSplit GetParsedSplit(string name, string defaultTreatment, bool killed = false, Dictionary<string, string> configurations = null, List<ConditionWithLogic> conditions = null, int? trafficAllocation = null, int? seed = null)
        {
            return new ParsedSplit
            {
                algo = AlgorithmEnum.Murmur,
                changeNumber = 1556063594549,
                defaultTreatment = defaultTreatment,
                killed = killed,
                name = name,
                seed = seed ?? 2095087412,
                trafficAllocation = trafficAllocation ?? 100,
                trafficAllocationSeed = -1953939473,
                trafficTypeName = "user",
                configurations = configurations,
                conditions = conditions ?? new List<ConditionWithLogic>
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
                }
            };
        }
    }
}