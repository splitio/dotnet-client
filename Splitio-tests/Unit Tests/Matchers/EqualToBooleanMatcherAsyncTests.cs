using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class EqualToBooleanMatcherAsyncTests
    {
        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingKey()
        {
            //Arrange
            var matcher = new EqualToBooleanMatcher(true);
            var matcher2 = new EqualToBooleanMatcher(false);

            //Act
            var result = await matcher.MatchAsync(true);
            var result2 = await matcher2.MatchAsync(false);

            //Assert
            Assert.IsTrue(result);
            Assert.IsTrue(result2);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingKey()
        {
            //Arrange
            var matcher = new EqualToBooleanMatcher(true);
            var matcher2 = new EqualToBooleanMatcher(false);

            //Act
            var result = await matcher.MatchAsync(false);
            var result2 = await matcher2.MatchAsync(true);

            //Assert
            Assert.IsFalse(result);
            Assert.IsFalse(result2);

        }


        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingLong()
        {
            //Arrange
            var matcher = new EqualToBooleanMatcher(true);

            //Act
            var result = await matcher.MatchAsync(123);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingDate()
        {
            //Arrange
            var matcher = new EqualToBooleanMatcher(true);

            //Act
            var result = await matcher.MatchAsync(DateTime.UtcNow);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingKey()
        {
            //Arrange
            var matcher = new EqualToBooleanMatcher(true);

            //Act
            var result = await matcher.MatchAsync(new Key("test", "test"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingStringNotBoolean()
        {
            //Arrange
            var matcher = new EqualToBooleanMatcher(true);

            //Act
            var result = await matcher.MatchAsync("testring");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueIfMatchingStringBoolean()
        {
            //Arrange
            var matcher = new EqualToBooleanMatcher(true);

            //Act
            var result = await matcher.MatchAsync("true");

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingSet()
        {
            //Arrange
            var matcher = new EqualToBooleanMatcher(true);

            //Act
            var keys = new List<string>
            {
                "test1",
                "test3"
            };

            var result = await matcher.MatchAsync(keys);

            //Assert
            Assert.IsFalse(result);
        }
    }
}
