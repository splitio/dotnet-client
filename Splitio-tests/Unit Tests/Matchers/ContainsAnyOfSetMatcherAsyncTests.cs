using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Splitio.Services.Parsing;
using Splitio.Domain;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class ContainsAnyOfSetMatcherAsyncTests
    {
        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnAllMatchingKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var keys = new List<string>
            {
                "test1",
                "test2",
                "test3"
            };

            var result = await matcher.MatchAsync(keys);

            //Assert
            Assert.IsTrue(result); //keys contains test1 and test2
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnAnyMatchingKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var keys = new List<string>
            {
                "test1",
                "test3"
            };

            var result = await matcher.MatchAsync(keys);

            //Assert
            Assert.IsTrue(result); //keys contains test1 
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNoneMatchingKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test0",
                "test2"
            };
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var keys = new List<string>
            {
                "test1",
                "test3"
            };

            var result = await matcher.MatchAsync(keys);

            //Assert
            Assert.IsFalse(result); //keys contains none of the elements
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyWhitelist()
        {
            //Arrange
            var toCompare = new List<string>();
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var keys = new List<string>
            {
                "test1",
                "test3"
            };

            var result = await matcher.MatchAsync(keys);

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
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            List<string> key = null;
            var result = await matcher.MatchAsync(key);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            List<string> key = new List<string>();
            var result = await matcher.MatchAsync(key);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingLong()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(123);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingDate()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(DateTime.UtcNow);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(new Key("test", "test"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingString()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync("test");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingBoolean()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(true);

            //Assert
            Assert.IsFalse(result);
        }
    }
}
