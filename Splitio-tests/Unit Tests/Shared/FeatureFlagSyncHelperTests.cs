using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Filters;
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
            var filter = new FlagSetsFilter(new HashSet<string> { "set_b" });
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

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, filter);

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<ParsedSplit>>(), It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChanges_WithFlagSetsConfig_ShouldRemove()
        {
            // Arrange.
            var filter = new FlagSetsFilter(new HashSet<string> { "set_x" });
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

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, filter);

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<ParsedSplit>>(), It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChanges_WithoutFlagSetsConfig_ShouldRemove()
        {
            // Arrange.
            var filter = new FlagSetsFilter(new HashSet<string> { "set_x" });
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

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, filter);

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<ParsedSplit>>(), It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChanges_WithFlagSetsConfig_ShouldAddOrUpdateAndRemove()
        {
            // Arrange.
            var filter = new FlagSetsFilter(new HashSet<string> { "set_x", "set_b", "set_c" });
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

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, filter);

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<ParsedSplit>>(), It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChanges_WithoutFlagSetsConfig_ShouldAddOrUpdate()
        {
            // Arrange.
            var filter = new FlagSetsFilter(new HashSet<string>());
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

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, filter);

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<ParsedSplit>>(), It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChanges_WithoutFlagSets_ShouldAddOrUpdate()
        {
            // Arrange.
            var filter = new FlagSetsFilter(new HashSet<string>());
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

            var helper = new FeatureFlagSyncHelper(_featureFlagParser.Object, _featureFlagsCache.Object, filter);

            // Act.
            var result = helper.UpdateFeatureFlagsFromChanges(featureFlags, 100);

            // Assert.
            Assert.IsFalse(result.Any());
            _featureFlagParser.Verify(mock => mock.Parse(It.IsAny<Split>()), Times.Exactly(5));
            _featureFlagsCache.Verify(mock => mock.Update(It.IsAny<List<ParsedSplit>>(), It.IsAny<List<ParsedSplit>>(), It.IsAny<long>()), Times.Once);
        }
    }
}
