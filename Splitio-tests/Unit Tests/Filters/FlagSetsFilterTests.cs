using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Filters;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Filters
{
    [TestClass]
    public class FlagSetsFilterTests
    {
        [TestMethod]
        public void SingleSetMatchWithoutSetsShouldReturnTrue()
        {
            // Arrange.
            var sets = new HashSet<string>();
            var flagSetsFilter = new FlagSetsFilter(sets);

            // Act & Assert.
            Assert.IsTrue(flagSetsFilter.Match("set_2"));
        }

        [TestMethod]
        public void SingleSetMatchWithEmptyShouldReturnFalse()
        {
            // Arrange.
            var sets = new HashSet<string> { "set_1", "set_2", "set_3" };
            var flagSetsFilter = new FlagSetsFilter(sets);

            // Act & Assert.
            Assert.IsFalse(flagSetsFilter.Match(""));
        }

        [TestMethod]
        public void SingleSetMatchShouldReturnTrue()
        {
            // Arrange.
            var sets = new HashSet<string> { "set_1", "set_2", "set_3" };
            var flagSetsFilter = new FlagSetsFilter(sets);

            // Act & Assert.
            Assert.IsTrue(flagSetsFilter.Match("set_2"));
        }

        [TestMethod]
        public void SingleSetMatchShouldReturnFalse()
        {
            // Arrange.
            var sets = new HashSet<string> { "set_1", "set_2", "set_3" };
            var flagSetsFilter = new FlagSetsFilter(sets);

            // Act & Assert.
            Assert.IsFalse(flagSetsFilter.Match("set_4"));
        }

        [TestMethod]
        public void MultipleSetsMatchWithoutSetsShouldReturnTrue()
        {
            // Arrange.
            var sets = new HashSet<string>();
            var flagSetsFilter = new FlagSetsFilter(sets);

            // Act & Assert.
            Assert.IsTrue(flagSetsFilter.Match(new HashSet<string> { "set_2" }));
        }

        [TestMethod]
        public void MultipleSetsMatchWithEmptyShouldReturnFalse()
        {
            // Arrange.
            var flagSetsFilter = new FlagSetsFilter(new HashSet<string> { "set_1", "set_2", "set_3" });
            HashSet<string> flagSets = null;

            // Act & Assert.
            Assert.IsFalse(flagSetsFilter.Match(flagSets));
        }

        [TestMethod]
        public void MultipleSetsMatchShouldReturnTrue()
        {
            // Arrange.
            var sets = new HashSet<string> { "set_1", "set_2", "set_3" };
            var flagSetsFilter = new FlagSetsFilter(sets);

            // Act & Assert.
            Assert.IsTrue(flagSetsFilter.Match(new HashSet<string> { "set_2" }));
        }

        [TestMethod]
        public void MultipleSetsMatchShouldReturnFalse()
        {
            // Arrange.
            var sets = new HashSet<string> { "set_1", "set_2", "set_3" };
            var flagSetsFilter = new FlagSetsFilter(sets);

            // Act & Assert.
            Assert.IsFalse(flagSetsFilter.Match(new HashSet<string> { "set_4" }));
        }
    }
}
