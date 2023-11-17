using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Client.Classes;
using Splitio.Services.Client.Interfaces;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Client
{
    [TestClass]
    public class SplitManagerAsyncTests
    {
        private readonly Mock<IBlockUntilReadyService> _blockUntilReadyService;
        private readonly Mock<IFeatureFlagCacheConsumer> _splitCache;
        private readonly string rootFilePath;

        private readonly ISplitManager _splitManager;

        public SplitManagerAsyncTests()
        {
            _blockUntilReadyService = new Mock<IBlockUntilReadyService>();
            _splitCache = new Mock<IFeatureFlagCacheConsumer>();

            _splitManager = new SplitManager(_splitCache.Object, _blockUntilReadyService.Object);

            // This line is to clean the warnings.
            rootFilePath = string.Empty;

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        public async Task SplitsReturnSuccessfully()
        {
            //Arrange            
            var conditionWithLogic = new ConditionWithLogic()
            {
                conditionType = ConditionType.WHITELIST,
                partitions = new List<PartitionDefinition>()
                {
                    new PartitionDefinition(){size = 100, treatment = "off"}
                }
            };

            var conditionWithLogic2 = new ConditionWithLogic()
            {
                conditionType = ConditionType.ROLLOUT,
                partitions = new List<PartitionDefinition>()
                {
                    new PartitionDefinition {size = 90, treatment = "on"},
                    new PartitionDefinition {size = 10, treatment = "off"}
                }
            };

            var conditionsWithLogic = new List<ConditionWithLogic>
            {
                conditionWithLogic,
                conditionWithLogic2
            };

            var splits = new List<ParsedSplit>
            {
                new ParsedSplit { name = "test1", changeNumber = 10000, killed = false, trafficTypeName = "user", seed = -1, conditions = conditionsWithLogic, defaultTreatment = "def", Sets = new HashSet<string> { "set1", "set2"} },
                new ParsedSplit { name = "test2", conditions = conditionsWithLogic },
                new ParsedSplit { name = "test3", conditions = conditionsWithLogic },
                new ParsedSplit { name = "test4", conditions = conditionsWithLogic },
                new ParsedSplit { name = "test5", conditions = conditionsWithLogic },
                new ParsedSplit { name = "test6", conditions = conditionsWithLogic }
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _splitCache
                .Setup(mock => mock.GetAllSplitsAsync())
                .ReturnsAsync(splits);

            //Act
            var result = await _splitManager.SplitsAsync();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(6, result.Count);

            var firstResult = result.Find(x => x.name == "test1");
            Assert.AreEqual("test1", firstResult.name);
            Assert.AreEqual(10000, firstResult.changeNumber);
            Assert.IsFalse(firstResult.killed);
            Assert.AreEqual("user", firstResult.trafficType);
            Assert.AreEqual(2, firstResult.treatments.Count);
            Assert.AreEqual("on", firstResult.treatments[0]);
            Assert.AreEqual("off", firstResult.treatments[1]);
            Assert.AreEqual("def", firstResult.defaultTreatment);
            Assert.AreEqual(2, firstResult.sets.Count);
            Assert.IsTrue(firstResult.sets.Contains("set1"));
            Assert.IsTrue(firstResult.sets.Contains("set2"));
            var test5 = result.Find(x => x.name == "test5");
            Assert.IsFalse(test5.sets.Any());
            Assert.IsFalse(test5.sets.Contains("set1"));
        }

        [TestMethod]
        public async Task SplitsReturnWithNoRolloutConditionSuccessfully()
        {
            //Arrange            
            var conditionWithLogic = new ConditionWithLogic()
            {
                conditionType = ConditionType.WHITELIST,
                partitions = new List<PartitionDefinition>()
                {
                    new PartitionDefinition(){size = 100, treatment = "on"}
                }
            };

            var conditionsWithLogic = new List<ConditionWithLogic>
            {
                conditionWithLogic
            };

            var splits = new List<ParsedSplit>
            {
                new ParsedSplit { name = "test1", changeNumber = 10000, killed = false, trafficTypeName = "user", seed = -1, conditions = conditionsWithLogic, defaultTreatment = "def", Sets = new HashSet<string> { "set1", "set2"} },
                new ParsedSplit { name = "test2", conditions = conditionsWithLogic },
                new ParsedSplit { name = "test3", conditions = conditionsWithLogic },
                new ParsedSplit { name = "test4", conditions = conditionsWithLogic },
                new ParsedSplit { name = "test5", conditions = conditionsWithLogic },
                new ParsedSplit { name = "test6", conditions = conditionsWithLogic }
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _splitCache
                .Setup(mock => mock.GetAllSplitsAsync())
                .ReturnsAsync(splits);

            //Act
            var result = await _splitManager.SplitsAsync();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(6, result.Count);

            var firstResult = result.Find(x => x.name == "test1");
            Assert.AreEqual("test1", firstResult.name);
            Assert.AreEqual(10000, firstResult.changeNumber);
            Assert.IsFalse(firstResult.killed);
            Assert.AreEqual("user", firstResult.trafficType);
            Assert.AreEqual(conditionWithLogic.partitions.Count, firstResult.treatments.Count);
            Assert.AreEqual("def", firstResult.defaultTreatment);
            Assert.AreEqual(2, firstResult.sets.Count);
            Assert.IsTrue(firstResult.sets.Contains("set1"));
            Assert.IsTrue(firstResult.sets.Contains("set2"));
            var test5 = result.Find(x => x.name == "test5");
            Assert.IsFalse(test5.sets.Any());
            Assert.IsFalse(test5.sets.Contains("set1"));
        }

        [TestMethod]
        public async Task SplitReturnSuccessfully()
        {
            //Arrange            
            var conditionWithLogic = new ConditionWithLogic()
            {
                conditionType = ConditionType.ROLLOUT,
                partitions = new List<PartitionDefinition>()
                {
                    new PartitionDefinition(){size = 90, treatment = "on"},
                    new PartitionDefinition(){size = 10, treatment = "off"}
                }
            };

            var conditionsWithLogic = new List<ConditionWithLogic>
            {
                conditionWithLogic
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _splitCache
                .Setup(mock => mock.GetSplitAsync("test1"))
                .ReturnsAsync(new ParsedSplit { name = "test1", changeNumber = 10000, killed = false, trafficTypeName = "user", seed = -1, conditions = conditionsWithLogic, defaultTreatment = "def" });

            //Act
            var result = await _splitManager.SplitAsync("test1");

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test1", result.name);
            Assert.AreEqual(10000, result.changeNumber);
            Assert.IsFalse(result.killed);
            Assert.AreEqual("user", result.trafficType);
            Assert.AreEqual(2, result.treatments.Count);
            Assert.AreEqual("on", result.treatments[0]);
            Assert.AreEqual("off", result.treatments[1]);
            Assert.AreEqual("def", result.defaultTreatment);
        }

        [TestMethod]
        public async Task SplitReturnRolloutConditionTreatmentsSuccessfully()
        {
            //Arrange
            var conditionWithLogic = new ConditionWithLogic()
            {
                conditionType = ConditionType.WHITELIST,
                partitions = new List<PartitionDefinition>()
                {
                    new PartitionDefinition(){size = 100, treatment = "on"},
                }
            };

            var conditionWithLogic2 = new ConditionWithLogic()
            {
                conditionType = ConditionType.ROLLOUT,
                partitions = new List<PartitionDefinition>()
                {
                    new PartitionDefinition(){size = 90, treatment = "on"},
                    new PartitionDefinition(){size = 10, treatment = "off"},
                }
            };

            var conditionsWithLogic = new List<ConditionWithLogic>
            {
                conditionWithLogic,
                conditionWithLogic2
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _splitCache
                .Setup(mock => mock.GetSplitAsync("test1"))
                .ReturnsAsync(new ParsedSplit { name = "test1", changeNumber = 10000, killed = false, trafficTypeName = "user", seed = -1, conditions = conditionsWithLogic, defaultTreatment = "def" });

            //Act
            var result = await _splitManager.SplitAsync("test1");

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test1", result.name);
            Assert.AreEqual(2, result.treatments.Count);
            Assert.AreEqual("on", result.treatments[0]);
            Assert.AreEqual("off", result.treatments[1]);
            Assert.AreEqual("def", result.defaultTreatment);
        }

        [TestMethod]
        public async Task SplitReturnDefaultTreatmentsWhenNoRolloutCondition()
        {
            //Arrange
            var conditionWithLogic = new ConditionWithLogic()
            {
                conditionType = ConditionType.WHITELIST,
                partitions = new List<PartitionDefinition>()
                {
                    new PartitionDefinition(){size = 100, treatment = "on"},
                }
            };

            var conditionsWithLogic = new List<ConditionWithLogic>
            {
                conditionWithLogic
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _splitCache
                .Setup(mock => mock.GetSplitAsync("test1"))
                .ReturnsAsync(new ParsedSplit { name = "test1", changeNumber = 10000, killed = false, trafficTypeName = "user", seed = -1, conditions = conditionsWithLogic, defaultTreatment = "def" });

            //Act
            var result = await _splitManager.SplitAsync("test1");

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test1", result.name);
            Assert.AreEqual(conditionWithLogic.partitions.Count, result.treatments.Count);
            Assert.AreEqual("def", result.defaultTreatment);
        }

        [TestMethod]
        public async Task SplitReturnsNullWhenInexistent()
        {
            //Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            //Act
            var result = await _splitManager.SplitAsync("test1");

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SplitReturnsNullWhenCacheIsNull()
        {
            //Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            var manager = new SplitManager(null, _blockUntilReadyService.Object);

            //Act
            var result = await manager.SplitAsync("test1");

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SplitsWhenCacheIsEmptyShouldReturnEmptyList()
        {
            //Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _splitCache
                .Setup(mock => mock.GetAllSplitsAsync())
                .ReturnsAsync(new List<ParsedSplit>());

            //Act
            var result = await _splitManager.SplitsAsync();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task SplitsWhenCacheIsNotInstancedShouldReturnNull()
        {
            //Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            var manager = new SplitManager(null, _blockUntilReadyService.Object);

            //Act
            var result = await manager.SplitsAsync();

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SplitWhenCacheIsNotInstancedShouldReturnNull()
        {
            //Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            var manager = new SplitManager(null, _blockUntilReadyService.Object);

            //Act
            var result = await manager.SplitAsync("name");

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SplitWithNullNameShouldReturnNull()
        {
            //Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            //Act
            var result = await _splitManager.SplitAsync(null);

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SplitNamessWhenCacheIsNotInstancedShouldReturnNull()
        {
            //Arrange
            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            var manager = new SplitManager(null, _blockUntilReadyService.Object);

            //Act
            var result = await manager.SplitNamesAsync();

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SplitNamesReturnSuccessfully()
        {
            //Arrange
            var conditionWithLogic = new ConditionWithLogic()
            {
                partitions = new List<PartitionDefinition>()
                {
                    new PartitionDefinition(){size = 100, treatment = "on"}
                }
            };

            var conditionsWithLogic = new List<ConditionWithLogic>
            {
                conditionWithLogic
            };

            var splitNames = new List<string>
            {
                "test1",
                "test2",
                "test3",
                "test4",
                "test5",
                "test6"
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _splitCache
                .Setup(mock => mock.GetSplitNamesAsync())
                .ReturnsAsync(splitNames);

            _splitManager.BlockUntilReady(1000);

            //Act
            var result = await _splitManager.SplitNamesAsync();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(6, result.Count);

            var firstResult = result.Find(x => x == "test1");
            Assert.AreEqual("test1", firstResult);
        }

        [TestMethod]
        public async Task Splits_WithConfigs_ReturnSuccessfully()
        {
            //Arrange
            var configurations = new Dictionary<string, string>
            {
                { "On", "\"Name = \"Test Config\"" }
            };

            var conditionWithLogic = new ConditionWithLogic()
            {
                partitions = new List<PartitionDefinition>()
                {
                    new PartitionDefinition(){size = 100, treatment = "on"}
                }
            };

            var conditionsWithLogic = new List<ConditionWithLogic>
            {
                conditionWithLogic
            };

            var splits = new List<ParsedSplit>
            {
                new ParsedSplit { name = "test1", changeNumber = 10000, killed = false, trafficTypeName = "user", seed = -1, conditions = conditionsWithLogic, configurations = configurations, defaultTreatment = "def" },
                new ParsedSplit { name = "test2", conditions = conditionsWithLogic, configurations = configurations, defaultTreatment = "def2" },
                new ParsedSplit { name = "test3", conditions = conditionsWithLogic, defaultTreatment = "def3" },
                new ParsedSplit { name = "test4", conditions = conditionsWithLogic },
                new ParsedSplit { name = "test5", conditions = conditionsWithLogic },
                new ParsedSplit { name = "test6", conditions = conditionsWithLogic }
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _splitCache
                .Setup(mock => mock.GetAllSplitsAsync())
                .ReturnsAsync(splits);

            _splitManager.BlockUntilReady(1000);

            //Act
            var result = await _splitManager.SplitsAsync();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(6, result.Count);
            var test1Result = result.Find(res => res.name == "test1");
            Assert.IsNotNull(test1Result.configs);
            Assert.AreEqual("def", test1Result.defaultTreatment);
            var test2Result = result.Find(res => res.name == "test2");
            Assert.IsNotNull(test2Result.configs);
            Assert.AreEqual("def2", test2Result.defaultTreatment);
            var test3Result = result.Find(res => res.name == "test3");
            Assert.IsNull(test3Result.configs);
            Assert.AreEqual("def3", test3Result.defaultTreatment);
        }

        [TestMethod]
        public async Task Split_WithConfigs_ReturnSuccessfully()
        {
            //Arrange
            var configurations = new Dictionary<string, string>
            {
                { "On", "\"Name = \"Test Config\"" }
            };

            var conditionWithLogic = new ConditionWithLogic()
            {
                partitions = new List<PartitionDefinition>()
                {
                    new PartitionDefinition(){size = 100, treatment = "on"}
                }
            };

            var conditionsWithLogic = new List<ConditionWithLogic>
            {
                conditionWithLogic
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            _splitCache
                .Setup(mock => mock.GetSplitAsync("test1"))
                .ReturnsAsync(new ParsedSplit { name = "test1", changeNumber = 10000, killed = false, trafficTypeName = "user", seed = -1, conditions = conditionsWithLogic, configurations = configurations, defaultTreatment = "def" });

            _splitCache
                .Setup(mock => mock.GetSplitAsync("test2"))
                .ReturnsAsync(new ParsedSplit { name = "test2", conditions = conditionsWithLogic, configurations = configurations, defaultTreatment = "def2" });

            _splitCache
                .Setup(mock => mock.GetSplitAsync("test3"))
                .ReturnsAsync(new ParsedSplit { name = "test3", conditions = conditionsWithLogic, defaultTreatment = "def3" });

            _splitManager.BlockUntilReady(1000);

            //Act
            var result1 = await _splitManager.SplitAsync("test1");
            var result2 = await _splitManager.SplitAsync("test2");
            var result3 = await _splitManager.SplitAsync("test3");

            //Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result1.configs);
            Assert.AreEqual("def", result1.defaultTreatment);
            Assert.IsNotNull(result2);
            Assert.IsNotNull(result2.configs);
            Assert.AreEqual("def2", result2.defaultTreatment);
            Assert.IsNotNull(result3);
            Assert.IsNull(result3.configs);
            Assert.AreEqual("def3", result3.defaultTreatment);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\split.yaml")]
        public async Task Split_WithLocalhostClient_WhenNameIsTestingSplitOn_ReturnsSplit()
        {
            // Arrange.
            var splitViewExpected = new SplitView
            {
                name = "testing_split_on",
                treatments = new List<string> { "on" }
            };

            var configurationOptions = new ConfigurationOptions
            {
                LocalhostFilePath = $"{rootFilePath}split.yaml"
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            var factory = new SplitFactory("localhost", configurationOptions);
            var manager = factory.Manager();
            manager.BlockUntilReady(1000);

            // Act.
            var splitViewResult = await manager.SplitAsync("testing_split_on");

            // Assert.
            Assert.AreEqual(splitViewExpected.name, splitViewResult.name);
            Assert.IsFalse(splitViewResult.killed);
            Assert.IsNull(splitViewResult.configs);
            Assert.IsNull(splitViewResult.trafficType);
            Assert.AreEqual(splitViewExpected.treatments.Count, splitViewResult.treatments.Count);
            foreach (var treatment in splitViewExpected.treatments)
            {
                Assert.IsNotNull(splitViewResult.treatments.FirstOrDefault(t => t == treatment));
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\split.yaml")]
        public async Task Split_WithLocalhostClient_WhenNameIsTestingSplitOnlyWl_ReturnsSplit()
        {
            // Arrange.
            var splitViewExpected = new SplitView
            {
                name = "testing_split_only_wl",
                treatments = new List<string> { "whitelisted" }
            };

            var configurationOptions = new ConfigurationOptions
            {
                LocalhostFilePath = $"{rootFilePath}split.yaml"
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            var factory = new SplitFactory("localhost", configurationOptions);
            var manager = factory.Manager();
            manager.BlockUntilReady(1000);

            // Act.
            var splitViewResult = await manager.SplitAsync("testing_split_only_wl");

            // Assert.
            Assert.AreEqual(splitViewExpected.name, splitViewResult.name);
            Assert.IsFalse(splitViewResult.killed);
            Assert.IsNull(splitViewResult.configs);
            Assert.IsNull(splitViewResult.trafficType);
            Assert.AreEqual(splitViewExpected.treatments.Count, splitViewResult.treatments.Count);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\split.yaml")]
        public async Task Split_WithLocalhostClient_WhenNameIsTestingSplitWithWl_ReturnsSplit()
        {
            // Arrange.
            var splitViewExpected = new SplitView
            {
                name = "testing_split_with_wl",
                treatments = new List<string> { "not_in_whitelist" },
                configs = new Dictionary<string, string>
                {
                    { "not_in_whitelist", "{\"color\": \"green\"}" },
                    { "multi_key_wl", "{\"color\": \"brown\"}" }
                }
            };

            var configurationOptions = new ConfigurationOptions
            {
                LocalhostFilePath = $"{rootFilePath}split.yaml"
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            var factory = new SplitFactory("localhost", configurationOptions);
            var manager = factory.Manager();
            manager.BlockUntilReady(1000);

            // Act.
            var splitViewResult = await manager.SplitAsync("testing_split_with_wl");

            // Assert.
            Assert.AreEqual(splitViewExpected.name, splitViewResult.name);
            Assert.IsFalse(splitViewResult.killed);
            Assert.IsNull(splitViewResult.trafficType);
            Assert.AreEqual(splitViewExpected.configs.Count, splitViewResult.configs.Count);
            foreach (var config in splitViewExpected.configs)
            {
                Assert.AreEqual(expected: config.Value, splitViewResult.configs[config.Key]);
            }

            Assert.AreEqual(splitViewExpected.treatments.Count, splitViewResult.treatments.Count);
            foreach (var treatment in splitViewExpected.treatments)
            {
                Assert.IsNotNull(splitViewResult.treatments.FirstOrDefault(t => t == treatment));
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\split.yaml")]
        public async Task Split_WithLocalhostClient_WhenNameIsTestingSplitOffWithConfig_ReturnsSplit()
        {
            // Arrange.
            var splitViewExpected = new SplitView
            {
                name = "testing_split_off_with_config",
                treatments = new List<string> { "off" },
                configs = new Dictionary<string, string>
                {
                    { "off", "{\"color\": \"green\"}" }
                },
                defaultTreatment = "control"
            };

            var configurationOptions = new ConfigurationOptions
            {
                LocalhostFilePath = $"{rootFilePath}split.yaml"
            };

            _blockUntilReadyService
                .Setup(mock => mock.IsSdkReady())
                .Returns(true);

            var factory = new SplitFactory("localhost", configurationOptions);
            var manager = factory.Manager();
            manager.BlockUntilReady(1000);

            // Act.
            var splitViewResult = await manager.SplitAsync("testing_split_off_with_config");

            // Assert.
            Assert.AreEqual(splitViewExpected.name, splitViewResult.name);
            Assert.IsFalse(splitViewResult.killed);
            Assert.IsNull(splitViewResult.trafficType);
            Assert.AreEqual(splitViewExpected.configs.Count, splitViewResult.configs.Count);
            Assert.AreEqual(splitViewExpected.defaultTreatment, splitViewResult.defaultTreatment);
            foreach (var config in splitViewExpected.configs)
            {
                Assert.AreEqual(expected: config.Value, splitViewResult.configs[config.Key]);
            }

            Assert.AreEqual(splitViewExpected.treatments.Count, splitViewResult.treatments.Count);
            foreach (var treatment in splitViewExpected.treatments)
            {
                Assert.IsNotNull(splitViewResult.treatments.FirstOrDefault(t => t == treatment));
            }
        }
    }
}
