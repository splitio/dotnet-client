using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests
{
    [TestClass]
    public class AttributeMatcherTests
    {
        [TestMethod]
        public void MatchShouldReturnTrueIfAttributeInAttributesIsMatching()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                Attribute = "card_number",
                Matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                Negate = false
            };

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };

            //Act
            var result = matcher.Match(null, attributes);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfAttributeInAttributesIsMatchingButResultIsNegated()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                Attribute = "card_number",
                Matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                Negate = true
            };

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };

            //Act
            var result = matcher.Match(null, attributes);

            //Assert
            Assert.IsFalse(result);
        }


        [TestMethod]
        public void MatchShouldReturnFalseIfAttributesDictionaryIsNull()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                Attribute = "card_number",
                Matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                Negate = false
            };

            //Act
            var result = matcher.Match(null, null);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfValueForAttributeIsNullAndKeyIsNull()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                Attribute = null,
                Matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                Negate = false
            };

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };

            //Act
            var result = matcher.Match(new Key(null, null), attributes);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfValueForAttributeIsNullAndKeyNotMatching()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                Attribute = null,
                Matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                Negate = false
            };

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };

            //Act
            var result = matcher.Match(new Key("1", "1"), attributes);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnTrueIfValueForAttributeIsNullAndKeyMatching()
        {
            //Arrange
            var matcher = new AttributeMatcher()
            {
                Attribute = null,
                Matcher = new EqualToMatcher(DataTypeEnum.NUMBER, 12012),
                Negate = false
            };

            var attributes = new Dictionary<string, object>
            {
                { "card_number", 12012 },
                { "card_type", "ABC" }
            };

            //Act
            var result = matcher.Match(new Key ("12012", "12012"), attributes);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchShouldReturnTrueIfValueBooleanOrStringBooleanMatching()
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
                Attribute = "test1",
                Matcher = new EqualToBooleanMatcher(true),
                Negate = false
            };

            foreach (var value in possibleValues)
            {
                var attributes = new Dictionary<string, object>
                {
                    { "test1", value }
                };

                //Act
                var result = matcher.Match(new Key("12012", "12012"), attributes);

                //Assert
                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfValueBooleanOrStringBooleanNotMatching()
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
                Attribute = "test1",
                Matcher = new EqualToBooleanMatcher(true),
                Negate = false
            };

            foreach (var value in possibleValues)
            {
                var attributes = new Dictionary<string, object>
                {
                    { "test1", value }
                };

                //Act
                var result = matcher.Match(new Key("12012", "12012"), attributes);

                //Assert
                Assert.IsFalse(result);
            }
        }
    }
}
