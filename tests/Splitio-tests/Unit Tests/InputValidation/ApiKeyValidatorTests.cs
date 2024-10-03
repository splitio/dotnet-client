using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.InputValidation.Classes;
using Splitio.Services.Logger;
using System;

namespace Splitio_Tests.Unit_Tests.InputValidation
{
    [TestClass]
    public class ApiKeyValidatorTests
    {
        private Mock<ISplitLogger> _log;
        private ApiKeyValidator apiKeyValidator;

        [TestInitialize]
        public void Initialize()
        {
            _log = new Mock<ISplitLogger>();

            apiKeyValidator = new ApiKeyValidator(_log.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "API Key must be set to initialize Split SDK.")]
        public void Validate_WhenApiKeyIsEmpty_RaiseExceptionAndLogOneError()
        {
            //Arrange
            var apiKey = string.Empty;

            //Act
            apiKeyValidator.Validate(apiKey);

            //Assert
            _log.Verify(mock => mock.Error($"factory instantiation: you passed and empty api_key, api_key must be a non-empty string."), Times.Once());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "API Key must be set to initialize Split SDK.")]
        public void Validate_WhenApiKeyIsNull_RaiseExceptionAndLogOneError()
        {
            //Arrange
            string apiKey = null;

            //Act
            apiKeyValidator.Validate(apiKey);

            //Assert
            _log.Verify(mock => mock.Error($"factory instantiation: you passed a null api_key, api_key must be a non-empty string."), Times.Once());
        }

        [TestMethod]
        public void Validate_WhenApiKeyHasValue_NoLog()
        {
            //Arrange
            string apiKey = "api_key";

            //Act
            apiKeyValidator.Validate(apiKey);

            //Assert
            _log.Verify(mock => mock.Error(It.IsAny<string>()), Times.Never());
        }
    }
}
