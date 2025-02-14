using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Classes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class DependencyMatcherAsyncTests
    {
        private readonly Mock<IEvaluator> _evaluator;

        public DependencyMatcherAsyncTests()
        {
            _evaluator = new Mock<IEvaluator>();
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingKey()
        {
            //Arrange
            var treatments = new List<string>() { "on" };
            var matcher = new DependencyMatcher("test1", treatments);
            var key = new Key("test", "test");

            _evaluator
                .Setup(mock => mock.EvaluateFeaturesAsync(Splitio.Enums.API.DependecyMatcherAsync, key, new List<string> { "test1" }, null, false))
                .ReturnsAsync(new List<ExpectedTreatmentResult> { new ExpectedTreatmentResult(new TreatmentResult("test1", "label", "on"), false) });

            //Act
            var result = await matcher.MatchAsync(key, null, _evaluator.Object);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingKey()
        {
            //Arrange
            var treatments = new List<string>() { "off" };
            var matcher = new DependencyMatcher("test1", treatments);
            var key = new Key("test", "test");

            _evaluator
                .Setup(mock => mock.EvaluateFeaturesAsync(Splitio.Enums.API.DependecyMatcherAsync, key, new List<string> { "test1" }, null, false))
                .ReturnsAsync(new List<ExpectedTreatmentResult> { new ExpectedTreatmentResult(new TreatmentResult("test1", "label", "on"), false) });

            //Act
            var result = await matcher.MatchAsync(key, null, _evaluator.Object);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfNullSplitClient()
        {
            //Arrange
            var treatments = new List<string>();
            var matcher = new DependencyMatcher("test1", treatments);
            IEvaluator evaluator = null;

            //Act
            var result = await matcher.MatchAsync(new Key("test2", "test2"), null, evaluator);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfEmptyTreatmentList()
        {
            //Arrange
            var treatments = new List<string>();
            var matcher = new DependencyMatcher("test1", treatments);
            var key = new Key("test2", "test2");

            _evaluator
                .Setup(mock => mock.EvaluateFeaturesAsync(Splitio.Enums.API.DependecyMatcherAsync, key, new List<string> { "test1" }, null, false))
                .ReturnsAsync(new List<ExpectedTreatmentResult> { new ExpectedTreatmentResult(new TreatmentResult("test1", "label", "on"), false) });

            //Act
            var result = await matcher.MatchAsync(key, null, _evaluator.Object);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingLong()
        {
            //Arrange
            var treatments = new List<string>() { "on" };
            var matcher = new DependencyMatcher("test1", treatments);
            var key = new Key("test2", "test2");

            _evaluator
                .Setup(mock => mock.EvaluateFeaturesAsync(Splitio.Enums.API.DependecyMatcherAsync, key, new List<string> { "test1" }, null, false))
                .ReturnsAsync(new List<ExpectedTreatmentResult> { new ExpectedTreatmentResult(new TreatmentResult("test1", "label", "on"), false) });

            //Act
            var result = await matcher.MatchAsync(123, null, _evaluator.Object);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingDate()
        {
            //Arrange
            var treatments = new List<string>() { "on" };
            var matcher = new DependencyMatcher("test1", treatments);
            var key = new Key("test2", "test2");

            _evaluator
                .Setup(mock => mock.EvaluateFeaturesAsync(Splitio.Enums.API.DependecyMatcherAsync, key, new List<string> { "test1" }, null, false))
                .ReturnsAsync(new List<ExpectedTreatmentResult> { new ExpectedTreatmentResult(new TreatmentResult("test1", "label", "on"), false) });

            //Act
            var result = await matcher.MatchAsync(DateTime.UtcNow, null, _evaluator.Object);

            //Assert
            Assert.IsFalse(result);
        }


        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingList()
        {
            //Arrange
            var treatments = new List<string>() { "on" };
            var matcher = new DependencyMatcher("test1", treatments);
            var key = new Key("test2", "test2");

            _evaluator
                .Setup(mock => mock.EvaluateFeaturesAsync(Splitio.Enums.API.DependecyMatcherAsync, key, new List<string> { "test1" }, null, false))
                .ReturnsAsync(new List<ExpectedTreatmentResult> { new ExpectedTreatmentResult(new TreatmentResult("test1", "label", "on"), false) });

            //Act
            var result = await matcher.MatchAsync(DateTime.UtcNow, null, _evaluator.Object);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingString()
        {
            //Arrange
            var treatments = new List<string>() { "on" };
            var matcher = new DependencyMatcher("test1", treatments);
            var key = new Key("test2", "test2");

            _evaluator
                .Setup(mock => mock.EvaluateFeaturesAsync(Splitio.Enums.API.DependecyMatcherAsync, key, new List<string> { "test1" }, null, false))
                .ReturnsAsync(new List<ExpectedTreatmentResult> { new ExpectedTreatmentResult(new TreatmentResult("test1", "label", "on"), false) });

            //Act
            var result = await matcher.MatchAsync("test", null, _evaluator.Object);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingBoolean()
        {
            //Arrange
            var treatments = new List<string>() { "on" };
            var matcher = new DependencyMatcher("test1", treatments);
            var key = new Key("test2", "test2");

            _evaluator
                .Setup(mock => mock.EvaluateFeaturesAsync(Splitio.Enums.API.DependecyMatcherAsync, key, new List<string> { "test1" }, null, false))
                .ReturnsAsync(new List<ExpectedTreatmentResult> { new ExpectedTreatmentResult(new TreatmentResult("test1", "label", "on"), false) });

            //Act
            var result = await matcher.MatchAsync(true, null, _evaluator.Object);

            //Assert
            Assert.IsFalse(result);
        }
    }
}
