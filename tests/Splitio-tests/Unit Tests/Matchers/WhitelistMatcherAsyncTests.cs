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
    public class WhitelistMatcherAsyncTests
    {
        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingKeyWithKey()
        {
            //Arrange
            var keys = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new WhitelistMatcher(keys);

            //Act
            var result = await matcher.MatchAsync(new Key("test2", "test2"));

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingKeyWithKey()
        {
            //Arrange
            var keys = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new WhitelistMatcher(keys);

            //Act
            var result = await matcher.MatchAsync(new Key("test3", "test3"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyWhitelistWithKey()
        {
            //Arrange
            var keys = new List<string>();
            var matcher = new WhitelistMatcher(keys);

            //Act
            var result = await matcher.MatchAsync(new Key("test2", "test2"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingLong()
        {
            //Arrange
            var keys = new List<string>();
            var matcher = new WhitelistMatcher(keys);

            //Act
            var result = await matcher.MatchAsync(123);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingDate()
        {
            //Arrange
            var keys = new List<string>();
            var matcher = new WhitelistMatcher(keys);

            //Act
            var result = await matcher.MatchAsync(DateTime.UtcNow);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingKey()
        {
            //Arrange
            var keys = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new WhitelistMatcher(keys);

            //Act
            var result = await matcher.MatchAsync("test2");

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingKey()
        {
            //Arrange
            var keys = new List<string> { "test1", "test2" };
            var matcher = new WhitelistMatcher(keys);

            //Act
            var result = await matcher.MatchAsync("test3");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyWhitelist()
        {
            //Arrange
            var keys = new List<string>();
            var matcher = new WhitelistMatcher(keys);

            //Act
            var result = await matcher.MatchAsync("test2");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingBoolean()
        {
            //Arrange
            var keys = new List<string>();
            var matcher = new WhitelistMatcher(keys);

            //Act
            var result = await matcher.MatchAsync(true);

            //Assert
            Assert.IsFalse(result);
        }
    }
}
