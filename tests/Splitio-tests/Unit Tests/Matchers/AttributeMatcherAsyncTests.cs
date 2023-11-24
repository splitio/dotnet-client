using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests
{
    [TestClass]
    public class AttributeMatcherAsyncTests
    {
        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueIfAttributeInAttributesIsMatching()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                attribute = "card_number",
                matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                negate = false
            };

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };

            //Act
            var result = await matcher.MatchAsync(null, attributes);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfAttributeInAttributesIsMatchingButResultIsNegated()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                attribute = "card_number",
                matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                negate = true
            };

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };

            //Act
            var result = await matcher.MatchAsync(null, attributes);

            //Assert
            Assert.IsFalse(result);
        }


        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfAttributesDictionaryIsNull()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                attribute = "card_number",
                matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                negate = false
            };

            //Act
            var result = await matcher.MatchAsync(null, null);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfValueForAttributeIsNullAndKeyIsNull()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                attribute = null,
                matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                negate = false
            };

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };

            //Act
            var result = await matcher.MatchAsync(new Key(null, null), attributes);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfValueForAttributeIsNullAndKeyNotMatching()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                attribute = null,
                matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                negate = false
            };

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };

            //Act
            var result = await matcher.MatchAsync(new Key("1", "1"), attributes);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueIfValueForAttributeIsNullAndKeyMatching()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                attribute = null,
                matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                negate = false
            };

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };

            //Act
            var result = await matcher.MatchAsync(new Key("12012", "12012"), attributes);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueIfValueBooleanOrStringBooleanMatching()
        {
            //Arrange
            var possibleValues = new List<object>
            {
                true,
                "true",
                "TRUE",
                "True",
                "TrUe",
                "truE"
            };

            var matcher = new AttributeMatcher()
            {
                attribute = "test1",
                matcher = new EqualToBooleanMatcher(true),
                negate = false
            };

            foreach (var value in possibleValues)
            {
                var attributes = new Dictionary<string, object>
                {
                    { "test1", value }
                };

                //Act
                var result = await matcher.MatchAsync(new Key("12012", "12012"), attributes);

                //Assert
                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfValueBooleanOrStringBooleanNotMatching()
        {
            //Arrange
            var possibleValues = new List<object>
            {
                false,
                "False",
                "test"
            };

            var matcher = new AttributeMatcher()
            {
                attribute = "test1",
                matcher = new EqualToBooleanMatcher(true),
                negate = false
            };

            foreach (var value in possibleValues)
            {
                var attributes = new Dictionary<string, object>
                {
                    { "test1", value }
                };

                //Act
                var result = await matcher.MatchAsync(new Key("12012", "12012"), attributes);

                //Assert
                Assert.IsFalse(result);
            }
        }
    }
}
