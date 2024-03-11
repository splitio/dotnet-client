using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Filters;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Splitio_Tests.Unit_Tests.Shared
{
    [TestClass]
    public class FeatureFlagSyncServiceTests
    {
        private readonly Mock<ISplitParser> _featureFlagParser;
        private readonly Mock<IFeatureFlagCacheProducer> _featureFlagsCache;
        private readonly Mock<IFlagSetsFilter> _flagSetsFilter;

        private readonly IFeatureFlagSyncService _featureFlagSyncService;

        public FeatureFlagSyncServiceTests()
        {
            _featureFlagParser = new Mock<ISplitParser>();
            _featureFlagsCache = new Mock<IFeatureFlagCacheProducer>();
            _flagSetsFilter = new Mock<IFlagSetsFilter>();

            _featureFlagSyncService = new FeatureFlagSyncService(_featureFlagParser.Object, _featureFlagsCache.Object, _flagSetsFilter.Object);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChangesSuccess()
        {
            // Arrange.
            var till = 10;
            var changes = new List<Split>
            {
                new Split { Name = "feature-flag-name-1", DefaultTreatment = "off", Conditions = new List<Condition>() },
                new Split { Name = "feature-flag-name-2", DefaultTreatment = "on", Conditions = new List<Condition>() },
                new Split { Name = "feature-flag-name-3", DefaultTreatment = "v1", Conditions = new List<Condition>() }
            };

            _featureFlagParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit());

            _flagSetsFilter
                .Setup(mock => mock.Intersect(It.IsAny<HashSet<string>>()))
                .Returns(true);

            // Act.
            var result = _featureFlagSyncService.UpdateFeatureFlagsFromChanges(changes, till);

            // Assert.
            Assert.IsFalse(result.Any());
            _flagSetsFilter.Verify(mock => mock.Intersect(It.IsAny<HashSet<string>>()), Times.Exactly(3));
            _featureFlagsCache.Verify(mock => mock.Update(It.Is<List<ParsedSplit>>(l => l.Count == 3), It.Is<List<string>>(l => l.Count == 0), It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChangesWithArchivedDefinition()
        {
            // Arrange.
            var till = 10;
            var changes = new List<Split>
            {
                new Split { Name = "feature-flag-name-1", DefaultTreatment = "off", Conditions = new List<Condition>() },
                new Split { Name = "feature-flag-name-2", DefaultTreatment = "on", Conditions = new List<Condition>() },
                new Split { Name = "feature-flag-name-3", DefaultTreatment = "v1", Conditions = new List<Condition>() }
            };

            _featureFlagParser
                .SetupSequence(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit())
                .Returns((ParsedSplit)null)
                .Returns(new ParsedSplit());

            _flagSetsFilter
                .Setup(mock => mock.Intersect(It.IsAny<HashSet<string>>()))
                .Returns(true);

            // Act.
            var result = _featureFlagSyncService.UpdateFeatureFlagsFromChanges(changes, till);

            // Assert.
            Assert.IsFalse(result.Any());
            _flagSetsFilter.Verify(mock => mock.Intersect(It.IsAny<HashSet<string>>()), Times.Exactly(2));
            _featureFlagsCache.Verify(mock => mock.Update(It.Is<List<ParsedSplit>>(l => l.Count == 2), It.Is<List<string>>(l => l.Count == 1), It.IsAny<long>()), Times.Once);
        }

        [TestMethod]
        public void UpdateFeatureFlagsFromChangesWithIntersectFalse()
        {
            // Arrange.
            var till = 10;
            var changes = new List<Split>
            {
                new Split { Name = "feature-flag-name-1", DefaultTreatment = "off", Conditions = new List<Condition>() },
                new Split { Name = "feature-flag-name-2", DefaultTreatment = "on", Conditions = new List<Condition>() },
                new Split { Name = "feature-flag-name-3", DefaultTreatment = "v1", Conditions = new List<Condition>() }
            };

            _featureFlagParser
                .Setup(mock => mock.Parse(It.IsAny<Split>()))
                .Returns(new ParsedSplit());

            _flagSetsFilter
                .SetupSequence(mock => mock.Intersect(It.IsAny<HashSet<string>>()))
                .Returns(true)
                .Returns(true)
                .Returns(false);

            // Act.
            var result = _featureFlagSyncService.UpdateFeatureFlagsFromChanges(changes, till);

            // Assert.
            Assert.IsFalse(result.Any());
            _flagSetsFilter.Verify(mock => mock.Intersect(It.IsAny<HashSet<string>>()), Times.Exactly(3));
            _featureFlagsCache.Verify(mock => mock.Update(It.Is<List<ParsedSplit>>(l => l.Count == 2), It.Is<List<string>>(l => l.Count == 1), It.IsAny<long>()), Times.Once);
        }
    }
}
