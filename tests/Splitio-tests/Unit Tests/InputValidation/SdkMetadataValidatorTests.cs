using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.InputValidation.Interfaces;

namespace Splitio_Tests.Unit_Tests.InputValidation
{
    [TestClass]
    public class SdkMetadataValidatorTests
    {
        private readonly ISdkMetadataValidator _sdkMetadataValidator;

        public SdkMetadataValidatorTests()
        {
            _sdkMetadataValidator = new SdkMetadataValidator();
        }

        [TestMethod]
        public void MachineNameValidationReturnsUnknown()
        {
            // Act.
            var result = _sdkMetadataValidator.MachineNameValidation("Test", "TEST-志");

            // Assert.
            Assert.AreEqual(Splitio.Constants.Gral.Unknown, result);
        }

        [TestMethod]
        public void MachineNameValidationWhenValueIsEmptyReturnsUnknown()
        {
            // Act.
            var result = _sdkMetadataValidator.MachineNameValidation("Test", string.Empty);

            // Assert.
            Assert.AreEqual(Splitio.Constants.Gral.Unknown, result);
        }

        [TestMethod]
        public void MachineNameValidationReturnsName()
        {
            // Arrange.
            var machineName = "TEST-SPLIT";

            // Act.
            var result = _sdkMetadataValidator.MachineNameValidation("Test", machineName);

            // Assert.
            Assert.AreEqual(machineName, result);
        }
    }
}
