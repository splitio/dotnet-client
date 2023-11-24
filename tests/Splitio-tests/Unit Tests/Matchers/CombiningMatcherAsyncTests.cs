using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class CombiningMatcherAsyncTests
    {
        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseWithNoDelegates()
        {
            //Arrange
            var matcher = new CombiningMatcher()
            {
                delegates = null,
                combiner = CombinerEnum.AND
            };

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };

            //Act
            var key = new Key("test", "test");
            var result = await matcher.MatchAsync(key, attributes);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueIfAllMatchersMatch()
        {
            //Arrange
            var attributeMatcher1 = new Mock<AttributeMatcher>();
            var attributeMatcher2 = new Mock<AttributeMatcher>();
            var attributeMatcher3 = new Mock<AttributeMatcher>();

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };
            var delegates = new List<AttributeMatcher>();
            var key = new Key("test", "test");

            attributeMatcher1
                .Setup(x => x.MatchAsync(key, attributes, null))
                .ReturnsAsync(true);

            attributeMatcher2
                .Setup(x => x.MatchAsync(key, attributes, null))
                .ReturnsAsync(true);

            attributeMatcher3
                .Setup(x => x.MatchAsync(key, attributes, null))
                .ReturnsAsync(true);

            delegates.Add(attributeMatcher1.Object);
            delegates.Add(attributeMatcher2.Object);
            delegates.Add(attributeMatcher3.Object);

            var matcher = new CombiningMatcher()
            {
                delegates = delegates,
                combiner = CombinerEnum.AND
            };

            //Act
            var result = await matcher.MatchAsync(key, attributes);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfAnyMatchersNoMatch()
        {
            //Arrange
            var attributeMatcher1 = new Mock<AttributeMatcher>();
            var attributeMatcher2 = new Mock<AttributeMatcher>();
            var attributeMatcher3 = new Mock<AttributeMatcher>();

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };
            var delegates = new List<AttributeMatcher>();
            var key = new Key("test", "test");

            attributeMatcher1
                .Setup(x => x.MatchAsync(key, attributes, null))
                .ReturnsAsync(true);

            attributeMatcher2
                .Setup(x => x.MatchAsync(key, attributes, null))
                .ReturnsAsync(false);

            attributeMatcher3
                .Setup(x => x.MatchAsync(key, attributes, null))
                .ReturnsAsync(true);

            delegates.Add(attributeMatcher1.Object);
            delegates.Add(attributeMatcher2.Object);
            delegates.Add(attributeMatcher3.Object);

            var matcher = new CombiningMatcher()
            {
                delegates = delegates,
                combiner = CombinerEnum.AND
            };

            //Act
            var result = await matcher.MatchAsync(key, attributes);

            //Assert
            Assert.IsFalse(result);
        }
    }
}
