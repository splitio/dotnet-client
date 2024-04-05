using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class BetweenSemverMatcherTests
    {
        #region Sync
        [TestMethod]
        public void MatchShouldReturnTrue()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("1.1.1", "3.3.3");

            // Act.
            var result = matcher.Match("2.2.2");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalse()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("1.1.1", "3.3.3");

            // Act.
            var result = matcher.Match("4.4.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchWithPreReleaseToShouldReturnTrue()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("1.1.1-rc.1.1.1", "1.1.1-rc.1.1.3");

            // Act.
            var result = matcher.Match("1.1.1-rc.1.1.2");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchWithPreReleaseToShouldReturnFalse()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("1.1.1-rc.1.1.1", "1.1.1-rc.1.1.3");

            // Act and Assert.
            Assert.IsFalse(matcher.Match("1.1.1-rc.2.1.2"));
            Assert.IsFalse(matcher.Match("1.1.1-rc.1.2.2"));
        }

        [TestMethod]
        public void MatchWithMetadataShouldReturnTrue()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("2.2.2+metadata-lalala", "3.4.5+metadata-lalala");

            // Act.
            var result = matcher.Match("3.0.0");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchWithMetadataShouldReturnFalse()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("2.2.2+metadata-lalala", "3.4.5+metadata-lalala");

            // Act and Assert.
            Assert.IsFalse(matcher.Match("3.5.0"));
            Assert.IsFalse(matcher.Match("1.5.0"));
        }

        [TestMethod]
        public void MatchWithStartTargetNullReturnFalse()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher(null, "3.4.5+metadata-lalala");

            // Act and Assert.
            Assert.IsFalse(matcher.Match("3.5.0"));
        }

        [TestMethod]
        public void MatchWithEndTargetNullReturnFalse()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("3.4.5+metadata-lalala", null);

            // Act and Assert.
            Assert.IsFalse(matcher.Match("1.5.0"));
        }

        [TestMethod]
        public void MatchWithKeyNullReturnFalse()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("2.2.2+metadata-lalala", "3.4.5+metadata-lalala");

            // Act and Assert.
            Assert.IsFalse(matcher.Match((string)null));
        }

        [TestMethod]
        public void MatchReturnFalse()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("2.2.2+metadata-lalala", "3.4.5+metadata-lalala");

            // Act and Assert.
            Assert.IsFalse(matcher.Match(10));
            Assert.IsFalse(matcher.Match(true));
            Assert.IsFalse(matcher.Match(new List<string> { "2.2.2" }));
            Assert.IsFalse(matcher.Match(DateTime.Now));
        }

        [TestMethod]
        public void Match()
        {
            // Arrange.
            object str = "2.2.3";
            object num = 10;
            object list = new List<string> { str.ToString() };
            object dt = DateTime.Now;
            object boolean = false;
            object str2 = "5.2.2-rc.1";

            var matcher = new BetweenSemverMatcher("2.2.2+metadata-lalala", "3.4.5+metadata-lalala");

            // Act and Assert.
            Assert.IsTrue(matcher.Match(str));
            Assert.IsFalse(matcher.Match(num));
            Assert.IsFalse(matcher.Match(list));
            Assert.IsFalse(matcher.Match(dt));
            Assert.IsFalse(matcher.Match(boolean));
            Assert.IsFalse(matcher.Match(str2));
        }
        #endregion

        #region Async
        [TestMethod]
        public async Task MatchShouldReturnTrueAsync()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("1.1.1", "3.3.3");

            // Act.
            var result = await matcher.MatchAsync("2.2.2");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseAsync()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("1.1.1", "3.3.3");

            // Act.
            var result = await matcher.MatchAsync("4.4.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchWithPreReleaseToShouldReturnTrueAsync()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("1.1.1-rc.1.1.1", "1.1.1-rc.1.1.3");

            // Act.
            var result = await matcher.MatchAsync("1.1.1-rc.1.1.2");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchWithPreReleaseToShouldReturnFalseAsync()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("1.1.1-rc.1.1.1", "1.1.1-rc.1.1.3");

            // Act and Assert.
            Assert.IsFalse(await matcher.MatchAsync("1.1.1-rc.2.1.2"));
            Assert.IsFalse(await matcher.MatchAsync("1.1.1-rc.1.2.2"));
        }

        [TestMethod]
        public async Task MatchWithMetadataShouldReturnTrueAsync()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("2.2.2+metadata-lalala", "3.4.5+metadata-lalala");

            // Act.
            var result = await matcher.MatchAsync("3.0.0");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchWithMetadataShouldReturnFalseAsync()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("2.2.2+metadata-lalala", "3.4.5+metadata-lalala");

            // Act and Assert.
            Assert.IsFalse(await matcher.MatchAsync("3.5.0"));
            Assert.IsFalse(await matcher.MatchAsync("1.5.0"));
        }

        [TestMethod]
        public async Task MatchWithStartTargetNullReturnFalseAsync()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher(null, "3.4.5+metadata-lalala");

            // Act and Assert.
            Assert.IsFalse(await matcher.MatchAsync("3.5.0"));
        }

        [TestMethod]
        public async Task MatchWithEndTargetNullReturnFalseAsync()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("3.4.5+metadata-lalala", null);

            // Act and Assert.
            Assert.IsFalse(await matcher.MatchAsync("1.5.0"));
        }

        [TestMethod]
        public async Task MatchWithKeyNullReturnFalseAsync()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("2.2.2+metadata-lalala", "3.4.5+metadata-lalala");

            // Act and Assert.
            Assert.IsFalse(await matcher.MatchAsync((string)null));
        }

        [TestMethod]
        public async Task MatchReturnFalseAsync()
        {
            // Arrange.
            var matcher = new BetweenSemverMatcher("2.2.2+metadata-lalala", "3.4.5+metadata-lalala");

            // Act and Assert.
            Assert.IsFalse(await matcher.MatchAsync(10));
            Assert.IsFalse(await matcher.MatchAsync(true));
            Assert.IsFalse(await matcher.MatchAsync(new List<string> { "2.2.2" }));
            Assert.IsFalse(await matcher.MatchAsync(DateTime.Now));
        }

        [TestMethod]
        public async Task MatchAsync()
        {
            // Arrange.
            object str = "2.2.3";
            object num = 10;
            object list = new List<string> { str.ToString() };
            object dt = DateTime.Now;
            object boolean = false;
            object str2 = "5.2.2-rc.1";

            var matcher = new BetweenSemverMatcher("2.2.2+metadata-lalala", "3.4.5+metadata-lalala");

            // Act and Assert.
            Assert.IsTrue(await matcher.MatchAsync(str));
            Assert.IsFalse(await matcher.MatchAsync(num));
            Assert.IsFalse(await matcher.MatchAsync(list));
            Assert.IsFalse(await matcher.MatchAsync(dt));
            Assert.IsFalse(await matcher.MatchAsync(boolean));
            Assert.IsFalse(await matcher.MatchAsync(str2));
        }
        #endregion
    }
}
