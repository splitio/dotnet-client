using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class EqualToSetMatcherAsyncTests
    {
        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingKey()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new EqualToSetMatcher(toCompare);

            //Act
            var keys = new List<string> { "test1", "test2" };

            var result = await matcher.MatchAsync(keys);

            //Assert
            Assert.IsTrue(result); //keys contains test1 and test2
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnPartiallyMatchingKey()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new EqualToSetMatcher(toCompare);

            //Act
            var keys = new List<string>
            {
                "test1",
                "test2",
                "test3"
            };

            var result = await matcher.MatchAsync(keys);

            //Assert
            Assert.IsFalse(result); //keys contains test1 and test2 but whitelist not contains tests 3
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnAnyMatchingKey()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new EqualToSetMatcher(toCompare);

            //Act
            var keys = new List<string>
            {
                "test1",
                "test3"
            };

            var result = await matcher.MatchAsync(keys);

            //Assert
            Assert.IsFalse(result); //keys contains test1 but not test2
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingKey()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new EqualToSetMatcher(toCompare);

            //Act
            var keys = new List<string>
            {
                "test4",
                "test3"
            };

            var result = await matcher.MatchAsync(keys);

            //Assert
            Assert.IsFalse(result); //keys contains no elements from set
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyWhitelist()
        {
            //Arrange
            var toCompare = new List<string>();
            var matcher = new EqualToSetMatcher(toCompare);

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
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new EqualToSetMatcher(toCompare);

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
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new EqualToSetMatcher(toCompare);

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
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new EqualToSetMatcher(toCompare);

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
            var matcher = new EqualToSetMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(DateTime.UtcNow);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingKey()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new EqualToSetMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(new Key("test", "test"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingString()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new EqualToSetMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync("test");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingBoolean()
        {
            //Arrange
            var toCompare = new List<string> { "test1", "test2" };
            var matcher = new EqualToSetMatcher(toCompare);

            //Act
            var result = await matcher.MatchAsync(true);

            //Assert
            Assert.IsFalse(result);
        }
    }
}
