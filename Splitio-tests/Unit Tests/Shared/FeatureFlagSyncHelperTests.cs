using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.Linq;

namespace Splitio_Tests.Unit_Tests.Shared
{
    [TestClass]
    public class FeatureFlagSyncHelperTests
    {
        private Mock<ISplitParser> _featureFlagParser;
        private Mock<ISplitCache> _featureFlagsCache;

        [TestInitialize]
        public void Setup()
        {
            _featureFlagParser = new Mock<ISplitParser>();
            _featureFlagsCache = new Mock<ISplitCache>();
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChanges_WithFlagSetsConfig_ShouldAddOrUpdate()
        {
            // Arrange.
            var featureFlags = new List<Split>();
            for (int i = 0; i < 5; i++)
            {
                featureFlags.Add(new Split
                {
                    name = $"feature_flag_{i}",
                    Sets = new HashSet<string> { "set_a", "set_b", "set_c" },
                    conditions = new List<ConditionDefinition>()
                });
            }

            _featureFlagParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit());

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, new HashSet<string> { "set_b" });

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.RemoveSplit(It.IsAny<string>()), Times.Never);
            _featureFlagsCache.Verify(mock => mock.AddOrUpdate(It.IsAny<string>(), It.IsAny<SplitBase>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChanges_WithFlagSetsConfig_ShouldRemove()
        {
            // Arrange.
            var featureFlags = new List<Split>();
            for (int i = 0; i < 5; i++)
            {
                featureFlags.Add(new Split
                {
                    name = $"feature_flag_{i}",
                    Sets = new HashSet<string> { "set_a", "set_b", "set_c" },
                    conditions = new List<ConditionDefinition>()
                });
            }

            _featureFlagParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit());

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, new HashSet<string> { "set_x" });

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.RemoveSplit(It.IsAny<string>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.AddOrUpdate(It.IsAny<string>(), It.IsAny<SplitBase>()), Times.Never);
            _featureFlagsCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChanges_WithoutFlagSetsConfig_ShouldRemove()
        {
            // Arrange.
            var featureFlags = new List<Split>();
            for (int i = 0; i < 5; i++)
            {
                featureFlags.Add(new Split
                {
                    name = $"feature_flag_{i}",
                    conditions = new List<ConditionDefinition>()
                });
            }

            _featureFlagParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit());

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, new HashSet<string> { "set_x" });

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.RemoveSplit(It.IsAny<string>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.AddOrUpdate(It.IsAny<string>(), It.IsAny<SplitBase>()), Times.Never);
            _featureFlagsCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChanges_WithFlagSetsConfig_ShouldAddOrUpdateAndRemove()
        {
            // Arrange.
            var featureFlags = new List<Split>();
            for (int i = 0; i < 5; i++)
            {
                featureFlags.Add(new Split
                {
                    name = $"feature_flag_{i}",
                    conditions = new List<ConditionDefinition>(),
                    Sets = i%2 == 0 ? new HashSet<string> { "set_b", "set_h" } : new HashSet<string>()
                });
            }

            _featureFlagParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit());

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, new HashSet<string> { "set_x", "set_b", "set_c" });

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.RemoveSplit(It.IsAny<string>()), Times.Exactly(2));
            _featureFlagsCache.Verify(mock => mock.AddOrUpdate(It.IsAny<string>(), It.IsAny<SplitBase>()), Times.Exactly(3));
            _featureFlagsCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChanges_WithoutFlagSetsConfig_ShouldAddOrUpdate()
        {
            // Arrange.
            var featureFlags = new List<Split>();
            for (int i = 0; i < 5; i++)
            {
                featureFlags.Add(new Split
                {
                    name = $"feature_flag_{i}",
                    conditions = new List<ConditionDefinition>(),
                });
            }

            _featureFlagParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit());

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, new HashSet<string>());

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.RemoveSplit(It.IsAny<string>()), Times.Exactly(0));
            _featureFlagsCache.Verify(mock => mock.AddOrUpdate(It.IsAny<string>(), It.IsAny<SplitBase>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChanges_WithoutFlagSets_ShouldAddOrUpdate()
        {
            // Arrange.
            var featureFlags = new List<Split>();
            for (int i = 0; i < 5; i++)
            {
                featureFlags.Add(new Split
                {
                    name = $"feature_flag_{i}",
                    conditions = new List<ConditionDefinition>(),
                    Sets = new HashSet<string> { "set_a", "set_b", "set_c" },
                });
            }

            _featureFlagParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit());

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, new HashSet<string>());

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.RemoveSplit(It.IsAny<string>()), Times.Exactly(0));
            _featureFlagsCache.Verify(mock => mock.AddOrUpdate(It.IsAny<string>(), It.IsAny<SplitBase>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.SetChangeNumber(It.IsAny<long>()), Times.Once);
        }
    }
}
