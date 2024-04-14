using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Parsing.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class InListSemverMatcherTests
    {
        #region Sync
        [TestMethod]
        public void MatchShouldReturnTrue()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1", "2.2.2+beta", "3.3.3-rc1" });

            // Act.
            var result = matcher.Match("2.2.2+beta");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalse()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1", "2.2.2+beta", "3.3.3-rc1" });

            // Act.
            var result = matcher.Match("2.2.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchWithPreReleaseToShouldReturnTrue()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1-rc.1", "2.2.2-rc.3+beta", "3.3.3-rc.11" });

            // Act.
            var result = matcher.Match("1.1.1-rc.1");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchWithPreReleaseToShouldReturnFalse()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1-rc.1", "2.2.2-rc.3+beta", "3.3.3-rc.11" });

            // Act.
            var result = matcher.Match("1.1.1-rc.10");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchWithMetadataShouldReturnTrue()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1+meta", "2.2.2-rc.3+beta", "3.3.3+meta.2" });

            // Act.
            var result = matcher.Match("3.3.3+meta.2");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchWithMetadataShouldReturnFalse()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1+meta", "2.2.2-rc.3+beta", "3.3.3+meta.2" });

            // Act.
            var result = matcher.Match("3.3.3+metadata.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchWithTargetNullReturnFalse()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(null);

            // Act.
            var result = matcher.Match("3.3.3+metadata.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchWithEmptyListReturnFalse()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string>());

            // Act.
            var result = matcher.Match("3.3.3+metadata.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchWithKeyNullReturnFalse()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1+meta", "2.2.2-rc.3+beta", "3.3.3+meta.2" });

            // Act.
            var result = matcher.Match((string)null);

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchReturnFalse()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1+meta", "2.2.2-rc.3+beta", "3.3.3+meta.2" });

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
            object list = new List<string> { "value" };
            object dt = DateTime.Now;
            object boolean = false;
            object str2 = "2.2.2-rc.1";

            var matcher = new InListSemverMatcher(new List<string> { str.ToString(), "2.2.2-rc.3+beta", "3.3.3+meta.2" });

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
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1", "2.2.2+beta", "3.3.3-rc1" });

            // Act.
            var result = await matcher.MatchAsync("2.2.2+beta");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseAsync()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1", "2.2.2+beta", "3.3.3-rc1" });

            // Act.
            var result = await matcher.MatchAsync("2.2.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchWithPreReleaseToShouldReturnTrueAsync()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1-rc.1", "2.2.2-rc.3+beta", "3.3.3-rc.11" });

            // Act.
            var result = await matcher.MatchAsync("1.1.1-rc.1");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchWithPreReleaseToShouldReturnFalseAsync()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1-rc.1", "2.2.2-rc.3+beta", "3.3.3-rc.11" });

            // Act.
            var result = await matcher.MatchAsync("1.1.1-rc.10");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchWithMetadataShouldReturnTrueAsync()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1+meta", "2.2.2-rc.3+beta", "3.3.3+meta.2" });

            // Act.
            var result = await matcher.MatchAsync("3.3.3+meta.2");

            // Assert.
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchWithMetadataShouldReturnFalseAsync()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1+meta", "2.2.2-rc.3+beta", "3.3.3+meta.2" });

            // Act.
            var result = await matcher.MatchAsync("3.3.3+metadata.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchWithTargetNullReturnFalseAsync()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(null);

            // Act.
            var result = await matcher.MatchAsync("3.3.3+metadata.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchWithEmptyListReturnFalseAsync()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string>());

            // Act.
            var result = await matcher.MatchAsync("3.3.3+metadata.2");

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchWithKeyNullReturnFalseAsync()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1+meta", "2.2.2-rc.3+beta", "3.3.3+meta.2" });

            // Act.
            var result = await matcher.MatchAsync((string)null);

            // Assert.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchReturnFalseAsync()
        {
            // Arrange.
            var matcher = new InListSemverMatcher(new List<string> { "1.1.1+meta", "2.2.2-rc.3+beta", "3.3.3+meta.2" });

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
            object list = new List<string> { "value" };
            object dt = DateTime.Now;
            object boolean = false;
            object str2 = "2.2.2-rc.1";

            var matcher = new InListSemverMatcher(new List<string> { str.ToString(), "2.2.2-rc.3+beta", "3.3.3+meta.2" });

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
