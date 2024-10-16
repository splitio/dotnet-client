﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing;
using System;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests
{
    [TestClass]
    public class ContainsAnyOfSetMatcherTests
    {
        [TestMethod]
        public void MatchShouldReturnTrueOnAllMatchingKey()
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

            var result = matcher.Match(keys);

            //Assert
            Assert.IsTrue(result); //keys contains test1 and test2
        }

        [TestMethod]
        public void MatchShouldReturnTrueOnAnyMatchingKey()
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

            var result = matcher.Match(keys);

            //Assert
            Assert.IsTrue(result); //keys contains test1 
        }

        [TestMethod]
        public void MatchShouldReturnFalseOnNoneMatchingKey()
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

            var result = matcher.Match(keys);

            //Assert
            Assert.IsFalse(result); //keys contains none of the elements
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfEmptyWhitelist()
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

            var result = matcher.Match(keys);

            //Assert
            Assert.IsFalse(result); //Empty whitelist
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfNullKey()
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
            var result = matcher.Match(key);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfEmptyKey()
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
            var result = matcher.Match(key);

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
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

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
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var result = matcher.Match(DateTime.UtcNow);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfMatchingKey()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var result = matcher.Match(new Key("test", "test"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfMatchingString()
        {
            //Arrange
            var toCompare = new List<string>
            {
                "test1",
                "test2"
            };
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var result = matcher.Match("test");

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
            var matcher = new ContainsAnyOfSetMatcher(toCompare);

            //Act
            var result = matcher.Match(true);

            //Assert
            Assert.IsFalse(result);
        }
    }
}
