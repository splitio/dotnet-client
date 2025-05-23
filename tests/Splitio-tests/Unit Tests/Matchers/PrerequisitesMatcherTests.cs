using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Enums;
using Splitio.Services.Evaluator;
using Splitio.Services.Parsing.Matchers;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class PrerequisitesMatcherTests
    {
        private readonly Mock<IEvaluator> _evaluator;

        public PrerequisitesMatcherTests()
        {
            _evaluator = new Mock<IEvaluator>();
        }

        [TestMethod]
        public void ShouldReturnTrueWhenAllPrerequisitesAreMet()
        {
            // Arrange
            var key = new Key("a-key", null);
            _evaluator
                .Setup(x => x.EvaluateFeatures(API.Prerequisites, key, new List<string> { "always-on" }, null, false))
                .Returns(new List<TreatmentResult>
                {
                    new TreatmentResult("always-on", "on-labe-test", "on", false, 100)
                });

            _evaluator
                .Setup(x => x.EvaluateFeatures(API.Prerequisites, key, new List<string> { "always-off" }, null, false))
                .Returns(new List<TreatmentResult>
                {
                    new TreatmentResult("always-off", "off-labe-test", "off", false, 100)
                });

            // Single prerequisite - returns true
            var matcher = new PrerequisitesMatcher(new List<PrerequisitesDto>
            {
                new PrerequisitesDto { FeatureFlagName = "always-on", Treatments = new List<string> { "not-existing", "on", "other" } }
            });
            Assert.IsTrue(matcher.Match(key, null, _evaluator.Object));

            // Single prerequisite - returns false
            matcher = new PrerequisitesMatcher(new List<PrerequisitesDto>
            {
                new PrerequisitesDto { FeatureFlagName = "always-on", Treatments = new List<string> { "off", "v1" } }
            });
            Assert.IsFalse(matcher.Match(key, null, _evaluator.Object));

            // Multiple prerequisites - all met
            matcher = new PrerequisitesMatcher(new List<PrerequisitesDto>
            {
                new PrerequisitesDto { FeatureFlagName = "always-on", Treatments = new List<string> { "on" } },
                new PrerequisitesDto { FeatureFlagName = "always-off", Treatments = new List<string> { "off" } }
            });
            Assert.IsTrue(matcher.Match(key, null, _evaluator.Object));

            // Multiple prerequisites - one not met
            matcher = new PrerequisitesMatcher(new List<PrerequisitesDto>
            {
                new PrerequisitesDto { FeatureFlagName = "always-on", Treatments = new List<string> { "on" } },
                new PrerequisitesDto { FeatureFlagName = "always-off", Treatments = new List<string> { "on" } }
            });
            Assert.IsFalse(matcher.Match(key, null, _evaluator.Object));
        }

        [TestMethod]
        public void EdgeCases()
        {
            // Arrange
            var key = new Key("a-key", null);
            _evaluator
                .Setup(x => x.EvaluateFeatures(API.Prerequisites, key, new List<string> { "not-existent-feature-flag" }, null, false))
                .Returns(new List<TreatmentResult>());

            _evaluator
                .Setup(x => x.EvaluateFeatures(API.Prerequisites, key, new List<string> { "always-on" }, null, false))
                .Returns(new List<TreatmentResult>
                {
                    new TreatmentResult("always-on", "on-labe-test", "on", false, 100)
                });

            // No prerequisites
            var matcher = new PrerequisitesMatcher(null);
            Assert.IsTrue(matcher.Match(key, null, _evaluator.Object));

            matcher = new PrerequisitesMatcher(new List<PrerequisitesDto>());
            Assert.IsTrue(matcher.Match(key, null, _evaluator.Object));

            // Non-existent feature flag
            matcher = new PrerequisitesMatcher(new List<PrerequisitesDto>
            {
                new PrerequisitesDto { FeatureFlagName = "not-existent-feature-flag", Treatments = new List<string> { "on", "off" } }
            });
            Assert.IsFalse(matcher.Match(key, null, _evaluator.Object));

            // Empty treatments list
            matcher = new PrerequisitesMatcher(new List<PrerequisitesDto>
            {
                new PrerequisitesDto { FeatureFlagName = "always-on", Treatments = new List<string>() }
            });
            Assert.IsFalse(matcher.Match(key, null, _evaluator.Object));
        }
    }
}
