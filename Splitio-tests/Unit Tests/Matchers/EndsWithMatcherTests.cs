using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests
{
    [TestClass]
    public class EndsWithMatcherTests
    {
        [TestMethod]
        public async Task MatchShouldReturnTrueOnMatchingKeyString()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            var result = await matcher.Match("starttest1");

            //Assert
            Assert.IsTrue(result); //starttest1 ends with test1
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseOnNonMatchingKeyString()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            var result = await matcher.Match("starttest3");

            //Assert
            Assert.IsFalse(result); //key not ends with any element of whitelist
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseIfEmptyWhitelistString()
        {
            //Arrange
            var toCompare = new List<string>();
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            var result = await matcher.Match("test1");

            //Assert
            Assert.IsFalse(result); //Empty whitelist
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseIfNullKeyString()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            string key = null;
            var result = await matcher.Match(key);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseIfEmptyKeyString()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            string key = "";
            var result = await matcher.Match(key);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnTrueOnMatchingKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            var result = await matcher.Match(new Key("starttest1", "starttest1"));

            //Assert
            Assert.IsTrue(result); //starttest1 ends with test1
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseOnNonMatchingKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            var result = await matcher.Match(new Key("starttest3", "starttest3"));

            //Assert
            Assert.IsFalse(result); //key not ends with any element of whitelist
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseIfEmptyWhitelist()
        {
            //Arrange
            var toCompare = new List<string>();
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            var result = await matcher.Match(new Key("test1", "test1"));

            //Assert
            Assert.IsFalse(result); //Empty whitelist
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseIfNullKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            string key = null;
            var result = await matcher.Match(new Key(key, key));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseIfEmptyKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            string key = "";
            var result = await matcher.Match(new Key(key, key));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfMatchingLong()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            var result = matcher.Match(123);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfMatchingDate()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            var result = matcher.Match(DateTime.UtcNow);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfMatchingSet()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            var keys = new List<string>
            {
                "test1"
            };
            var result = matcher.Match(keys);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfMatchingBoolean()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new EndsWithMatcher(toCompare);

            //Act
            var result = matcher.Match(true);

            //Assert
            Assert.IsFalse(result);
        }
    }
}
