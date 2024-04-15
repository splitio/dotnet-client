using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Parsing.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class EqualToSemverMatcherTests
    {
        #region Sync
        [TestMethod]
        public void MatchShouldReturnFalse()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("1.1.1");

            // Act.
            var result = matcher.Match("1.1.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchEqualShouldReturnTrue()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("1.1.2");

            // Act.
            var result = matcher.Match("1.1.2");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchWithPreReleaseShouldReturnTrue()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("1.2.3----RC-SNAPSHOT.12.9.1--.12.88");

            // Act.
            var result = matcher.Match("1.2.3----RC-SNAPSHOT.12.9.1--.12.88");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchWithPreReleaseShouldReturnFalse()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("1.2.3----RC-SNAPSHOT.12.9.1--.12.88");

            // Act.
            var result = matcher.Match("1.2.3----RC-SNAPSHOT.12.9.1--.12.99");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchWithMetadataShouldReturnTrue()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("2.2.2-rc.2+metadata-lalala");

            // Act.
            var result = matcher.Match("2.2.2-rc.2+metadata-lalala");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchWithMetadataShouldReturnFalse()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("2.2.2-rc.2+metadata-lalala");

            // Act.
            var result = matcher.Match("2.2.2-rc.2+metadata");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchWithTargetNullReturnFalse()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher(null);

            // Act.
            var result = matcher.Match("2.2.2-rc.2+metadata");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchWithKeyNullReturnFalse()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("2.2.2-rc.2+metadata");

            // Act.
            var result = matcher.Match((string)null);

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchReturnFalse()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("2.2.2-rc.2+metadata");

            // Act and Assert.
            Assert.IsFalse(matcher.Match(10));
            Assert.IsFalse(matcher.Match(true));
            Assert.IsFalse(matcher.Match(new List<string> { "value" }));
            Assert.IsFalse(matcher.Match(DateTime.Now));
        }

        [TestMethod]
        public void Match()
        {
            // Arrange.
            object str = "2.2.2";
            object num = 10;
            object list = new List<string> { "value" };
            object dt = DateTime.Now;
            object boolean = false;
            object str2 = "2.2.2-rc.1";

            var matcher = new EqualToSemverMatcher("2.2.2");

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
        public async Task MatchShouldReturnFalseAsync()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("1.1.1");

            // Act.
            var result = await matcher.MatchAsync("1.1.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchEqualShouldReturnTrueAsync()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("1.1.2");

            // Act.
            var result = await matcher.MatchAsync("1.1.2");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchWithPreReleaseShouldReturnTrueAsync()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("1.2.3----RC-SNAPSHOT.12.9.1--.12.88");

            // Act.
            var result = await matcher.MatchAsync("1.2.3----RC-SNAPSHOT.12.9.1--.12.88");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchWithPreReleaseShouldReturnFalseAsync()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("1.2.3----RC-SNAPSHOT.12.9.1--.12.88");

            // Act.
            var result = await matcher.MatchAsync("1.2.3----RC-SNAPSHOT.12.9.1--.12.99");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchWithMetadataShouldReturnTrueAsync()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("2.2.2-rc.2+metadata-lalala");

            // Act.
            var result = await matcher.MatchAsync("2.2.2-rc.2+metadata-lalala");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchWithMetadataShouldReturnFalseAsync()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("2.2.2-rc.2+metadata-lalala");

            // Act.
            var result = await matcher.MatchAsync("2.2.2-rc.2+metadata");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchWithTargetNullReturnFalseAsync()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher(null);

            // Act.
            var result = await matcher.MatchAsync("2.2.2-rc.2+metadata");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchWithKeyNullReturnFalseAsync()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("2.2.2-rc.2+metadata");

            // Act.
            var result = await matcher.MatchAsync((string)null);

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchReturnFalseAsync()
        {
            // Arrange.
            var matcher = new EqualToSemverMatcher("2.2.2-rc.2+metadata");

            // Act and Assert.
            Assert.IsFalse(await matcher.MatchAsync(10));
            Assert.IsFalse(await matcher.MatchAsync(true));
            Assert.IsFalse(await matcher.MatchAsync(new List<string> { "value" }));
            Assert.IsFalse(await matcher.MatchAsync(DateTime.Now));
        }

        [TestMethod]
        public async Task MatchAsync()
        {
            // Arrange.
            object str = "2.2.2";
            object num = 10;
            object list = new List<string> { "value" };
            object dt = DateTime.Now;
            object boolean = false;
            object str2 = "2.2.2-rc.1";

            var matcher = new EqualToSemverMatcher("2.2.2");

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
