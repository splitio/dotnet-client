using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Filters;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.InputValidation.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Splitio_Tests.Unit_Tests.InputValidation
{
    [TestClass]
    public class FlagSetsValidatorTests
    {
        private IFlagSetsValidator _flagSetsValidator;

        public FlagSetsValidatorTests()
        {
            _flagSetsValidator = new FlagSetsValidator();
        }

        [TestMethod]
        public void CleanupSuccess()
        {
            // Arrange.
            var flagSets = new List<string> { "Hola", "hola", "@@@@", "tesT", " split ", "split io" };

            // Act.
            var result = _flagSetsValidator.Cleanup("Test", flagSets);

            // Assert.
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("hola"));
            Assert.IsTrue(result.Contains("@@@@"));
            Assert.IsTrue(result.Contains("test"));
            Assert.IsTrue(result.Contains("split"));
            Assert.IsTrue(result.Contains("split io"));
        }

        [TestMethod]
        public void ItemsSuccess()
        {
            // Arrange.
            var flagSets = new HashSet<string> { "hola", "@@@@", "test", "split", "split io" };

            // Act.
            var result = _flagSetsValidator.Items("Test", flagSets);

            // Assert.
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("hola"));
            Assert.IsTrue(result.Contains("test"));
            Assert.IsTrue(result.Contains("split"));
        }

        [TestMethod]
        public void ItemsWithFlagSetsFilterSuccess()
        {
            // Arrange.
            var flagSets = new HashSet<string> { "hola", "@@@@", "test", "split", "split io" };
            var filter = new FlagSetsFilter(new HashSet<string> { "test" });

            // Act.
            var result = _flagSetsValidator.Items("Test", flagSets, filter);

            // Assert.
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("test"));
        }

        [TestMethod]
        public void ItemsWithFlagSetsFilterFail()
        {
            // Arrange.
            var flagSets = new HashSet<string> { "hola", "@@@@", "test", "split", "split io" };
            var filter = new FlagSetsFilter(new HashSet<string> { "mauro" });

            // Act.
            var result = _flagSetsValidator.Items("Test", flagSets, filter);

            // Assert.
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void AreValidSuccess()
        {
            // Arrange.
            var flagSets = new List<string> { "Hola", "hola", "@@@@", "tesT", " split ", "split io" };

            // Act.
            var success = _flagSetsValidator.AreValid("Test", flagSets, null, out var result);

            // Assert.
            Assert.IsTrue(success);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("hola"));
            Assert.IsTrue(result.Contains("test"));
            Assert.IsTrue(result.Contains("split"));
        }

        [TestMethod]
        public void AreValidWithFlagSetsFilterSuccess()
        {
            // Arrange.
            var flagSets = new List<string> { "Hola", "hola", "@@@@", "tesT", " split ", "split io" };
            var filter = new FlagSetsFilter(new HashSet<string> { "test" });

            // Act.
            var success = _flagSetsValidator.AreValid("Test", flagSets, filter, out var result);

            // Assert.
            Assert.IsTrue(success);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("test"));
        }

        [TestMethod]
        public void AreValidWithFlagSetsFilterFail()
        {
            // Arrange.
            var flagSets = new List<string> { "Hola", "hola", "@@@@", "tesT", " split ", "split io" };
            var filter = new FlagSetsFilter(new HashSet<string> { "mauro" });

            // Act.
            var success = _flagSetsValidator.AreValid("Test", flagSets, filter, out var result);

            // Assert.
            Assert.IsFalse(success);
            Assert.IsFalse(result.Any());
        }
    }
}
