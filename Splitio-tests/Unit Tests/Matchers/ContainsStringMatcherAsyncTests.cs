using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class ContainsStringMatcherAsyncTests
    {
        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingKeyString()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync("test1");

            //Assert
            Assert.IsTrue(result); //keys contains test1
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnKeyContainingElementString()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync("abctest1abc");

            //Assert
            Assert.IsTrue(result); //keys contains test1
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingKeyString()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync("test3");

            //Assert
            Assert.IsFalse(result); //key not contains any element of whitelist
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyWhitelistString()
        {
            //Arrange
            var toCompare = new List<string>();
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync("test1");

            //Assert
            Assert.IsFalse(result); //Empty whitelist
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfNullKeyString()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            string key = null;
            var result = await matcher.MatchAsync(key);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyKeyString()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            string key = "";
            var result = await matcher.MatchAsync(key);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingKey()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(new Key("test1", "test1"));

            //Assert
            Assert.IsTrue(result); //keys contains test1
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnKeyContainingElement()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(new Key("abctest1abc", "abctest1abc"));

            //Assert
            Assert.IsTrue(result); //keys contains test1
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingKey()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(new Key("test3", "test3"));

            //Assert
            Assert.IsFalse(result); //key not contains any element of whitelist
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyWhitelist()
        {
            //Arrange
            var toCompare = new List<string>();
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(new Key("test1", "test1"));

            //Assert
            Assert.IsFalse(result); //Empty whitelist
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfNullKey()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            string key = null;
            var result = await matcher.MatchAsync(new Key(key, key));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyKey()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            string key = "";
            var result = await matcher.MatchAsync(new Key(key, key));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingLong()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(123);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingDate()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(DateTime.UtcNow);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingSet()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var keys = new List<string>();
            keys.Add("test1");
            var result = await matcher.MatchAsync(keys);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingBoolean()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new ContainsStringMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(true);

            //Assert
            Assert.IsFalse(result);
        }
    }
}
