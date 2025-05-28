using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Parsing;
using Splitio.Services.Parsing.Interfaces;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Shared
{
    [TestClass]
    public class RuleBasedSegmentUpdaterTests
    {
        private Mock<IParser<RuleBasedSegmentDto, RuleBasedSegment>> _parserMock;
        private Mock<IRuleBasedSegmentCache> _ruleBasedSegmentCacheMock;
        private Mock<ISegmentCache> _segmentCacheMock;
        private RuleBasedSegmentUpdater _updater;

        [TestInitialize]
        public void Initialize()
        {
            _parserMock = new Mock<IParser<RuleBasedSegmentDto, RuleBasedSegment>>();
            _ruleBasedSegmentCacheMock = new Mock<IRuleBasedSegmentCache>();
            _segmentCacheMock = new Mock<ISegmentCache>();

            _updater = new RuleBasedSegmentUpdater(_parserMock.Object, _ruleBasedSegmentCacheMock.Object);
        }

        [TestMethod]
        public void Process_EmptyChangesList_ReturnsEmptySegmentNames()
        {
            // Arrange
            var changes = new List<RuleBasedSegmentDto>();
            long till = 12345;

            // Act
            var result = _updater.Process(changes, till);

            // Assert
            Assert.AreEqual(0, result[Splitio.Enums.SegmentType.Standard].Count);
            _ruleBasedSegmentCacheMock.Verify(x => x.Update(It.IsAny<List<RuleBasedSegment>>(), It.IsAny<List<string>>(), till), Times.Once);
        }

        [TestMethod]
        public void Process_ValidSegments_AddsSegments()
        {
            // Arrange
            var changes = new List<RuleBasedSegmentDto>
            {
                new RuleBasedSegmentDto { Name = "segment1" },
                new RuleBasedSegmentDto { Name = "segment2" }
            };

            _parserMock
                .SetupSequence(x => x.Parse(It.IsAny<RuleBasedSegmentDto>(), _ruleBasedSegmentCacheMock.Object))
                .Returns(new RuleBasedSegment
                {
                    Name = "segment1",
                    Excluded = new Excluded(),
                    ChangeNumber = 1,
                    CombiningMatchers = new List<CombiningMatcher> 
                    {
                        new CombiningMatcher
                        {
                           delegates = new List<AttributeMatcher>
                           {
                               new AttributeMatcher
                               {
                                   matcher = new UserDefinedSegmentMatcher("seg1", _segmentCacheMock.Object)
                               }
                           }
                        }
                    }
                })
                .Returns(new RuleBasedSegment
                {
                    Name = "segment2",
                    Excluded = new Excluded(),
                    ChangeNumber = 1,
                    CombiningMatchers = new List<CombiningMatcher>
                    {
                        new CombiningMatcher
                        {
                           delegates = new List<AttributeMatcher>
                           {
                               new AttributeMatcher
                               {
                                   matcher = new UserDefinedSegmentMatcher("seg2", _segmentCacheMock.Object)
                               }
                           }
                        }
                    }
                });

            long till = 12345;

            // Act
            var result = _updater.Process(changes, till);

            // Assert
            var rSegments = result[Splitio.Enums.SegmentType.Standard];
            Assert.AreEqual(2, rSegments.Count);
            CollectionAssert.Contains(rSegments, "seg1");
            CollectionAssert.Contains(rSegments, "seg2");
            _ruleBasedSegmentCacheMock.Verify(x => x.Update(It.IsAny<List<RuleBasedSegment>>(), It.IsAny<List<string>>(), till), Times.Once);
        }

        [TestMethod]
        public void Process_InvalidSegments_RemovesSegments()
        {
            // Arrange
            var changes = new List<RuleBasedSegmentDto>
            {
                new RuleBasedSegmentDto { Name = "segment1" },
                new RuleBasedSegmentDto { Name = "segment2" }
            };

            _parserMock
                .Setup(x => x.Parse(It.IsAny<RuleBasedSegmentDto>(), _ruleBasedSegmentCacheMock.Object))
                .Returns((RuleBasedSegment)null);

            long till = 12345;

            // Act
            var result = _updater.Process(changes, till);

            // Assert
            Assert.AreEqual(0, result[Splitio.Enums.SegmentType.Standard].Count);
            _ruleBasedSegmentCacheMock.Verify(x => x.Update(It.IsAny<List<RuleBasedSegment>>(), It.IsAny<List<string>>(), till), Times.Once);
        }
    }
}
