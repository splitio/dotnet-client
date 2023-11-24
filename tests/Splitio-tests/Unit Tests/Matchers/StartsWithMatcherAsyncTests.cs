using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class StartsWithMatcherAsyncTests
    {
        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new StartsWithMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(new Key("test1end", "test1end"));

            //Assert
            Assert.IsTrue(result); //test1end starts with test1
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new StartsWithMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(new Key("test3end", "test3end"));

            //Assert
            Assert.IsFalse(result); //key not starts with any element of whitelist
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyWhitelist()
        {
            //Arrange
            var toCompare = new List<string>();
            var matcher = new StartsWithMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(new Key("test1", "test1"));

            //Assert
            Assert.IsFalse(result); //Empty whitelist
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfNullKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new StartsWithMatcher(toCompare);

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
            var matcher = new StartsWithMatcher(toCompare);

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
            var matcher = new StartsWithMatcher(toCompare);

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
            var matcher = new StartsWithMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(DateTime.UtcNow);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingSet()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new StartsWithMatcher(toCompare);

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
            var matcher = new StartsWithMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(true);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingKeyString()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new StartsWithMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync("test1end");

            //Assert
            Assert.IsTrue(result); //test1end starts with test1
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingKeyString()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new StartsWithMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync("test3end");

            //Assert
            Assert.IsFalse(result); //key not starts with any element of whitelist
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyWhitelistString()
        {
            //Arrange
            var toCompare = new List<string>();
            var matcher = new StartsWithMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync("test1");

            //Assert
            Assert.IsFalse(result); //Empty whitelist
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfNullKeyString()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new StartsWithMatcher(toCompare);

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
            var matcher = new StartsWithMatcher(toCompare);

            //Act
            string key = "";
            var result = await matcher.MatchAsync(key);

            //Assert
            Assert.IsFalse(result);
        }
    }
}
