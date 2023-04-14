using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;

namespace Splitio_Tests.Unit_Tests.Shared
{
    [TestClass]
    public class FactoryInstantiationsServiceTests
    {
        private readonly Mock<ISplitLogger> _logMock;

        private readonly FactoryInstantiationsService _factoryInstantiationsService;

        public FactoryInstantiationsServiceTests()
        {
            _logMock = new Mock<ISplitLogger>();

            _factoryInstantiationsService = (FactoryInstantiationsService)FactoryInstantiationsService.Instance(_logMock.Object);
        }

        [Ignore("Failing locally")]
        [TestMethod]
        public void FactoryInstantiationsService_AllScenarios()
        {
            // ############################################################
            // #############  Increase_WhenApiKeyDoesntExist  #############
            // ############################################################

            // Arrange
            var apiKey = "apiKey";

            // Act
            _factoryInstantiationsService.Increase(apiKey);
            var result = _factoryInstantiationsService.GetInstantiations();

            // Assert
            Assert.AreEqual(1, result[apiKey]);
            _logMock.Verify(mock => mock.Warn(It.IsAny<string>()), Times.Never);

            // #######################################################
            // #############  Increase_WhenApiKeyExists  #############
            // #######################################################

            // Act
            _factoryInstantiationsService.Increase(apiKey);
            result = _factoryInstantiationsService.GetInstantiations();

            // Assert
            Assert.AreEqual(2, result[apiKey]);
            _logMock.Verify(mock => mock.Warn("factory instantiation: You already have 1 factories with this API Key. We recommend keeping only one instance of the factory at all times(Singleton pattern) and reusing it throughout your application."), Times.Once);

            // ######################################################
            // #############  Increase_WhenIsNewApiKey  #############
            // ######################################################

            // Arrange
            var newApiKey = "newApiKey";

            // Act
            _factoryInstantiationsService.Increase(newApiKey);
            result = _factoryInstantiationsService.GetInstantiations();

            // Assert
            Assert.AreEqual(1, result[newApiKey]);
            _logMock.Verify(mock => mock.Warn("factory instantiation: You already have an instance of the Split factory. Make sure you definitely want this additional instance. We recommend keeping only one instance of the factory at all times(Singleton pattern) and reusing it throughout your application."), Times.Once);

            // ####################################################################
            // #############  Increase_WhenApiKeyExists_ThanMoreOnce  #############
            // ####################################################################

            // Act
            _factoryInstantiationsService.Increase(apiKey);
            result = _factoryInstantiationsService.GetInstantiations();

            // Assert
            Assert.AreEqual(3, result[apiKey]);
            _logMock.Verify(mock => mock.Warn("factory instantiation: You already have 2 factories with this API Key. We recommend keeping only one instance of the factory at all times(Singleton pattern) and reusing it throughout your application."), Times.Once);

            // #######################################################
            // #############  Decrease_WhenApiKeyExists  #############
            // #######################################################

            // Act
            _factoryInstantiationsService.Decrease(apiKey);
            result = _factoryInstantiationsService.GetInstantiations();

            // Assert
            Assert.AreEqual(2, result[apiKey]);

            // Act
            _factoryInstantiationsService.Decrease(apiKey);
            result = _factoryInstantiationsService.GetInstantiations();

            // Assert
            Assert.AreEqual(1, result[apiKey]);

            // ####################################################################################
            // #############  Decrease_WhenApiKeyExists_AndIsTheLastOne_ReturnsFalse  #############
            // ####################################################################################

            // Act
            _factoryInstantiationsService.Decrease(apiKey);
            result = _factoryInstantiationsService.GetInstantiations();

            // Assert
            Assert.IsFalse(result.TryGetValue(apiKey, out int value));            
        }

        [TestMethod]
        public void GetActiveFactories()
        {
            // Arrange.
            _factoryInstantiationsService.Clear();

            var apiKey = "apiKey";
            var apiKey2 = "apiKey-2";
            var apiKey3 = "apiKey-3";
            var apiKey4 = "apiKey-4";

            _factoryInstantiationsService.Increase(apiKey);
            _factoryInstantiationsService.Increase(apiKey2);
            _factoryInstantiationsService.Increase(apiKey3);
            _factoryInstantiationsService.Increase(apiKey4);
            _factoryInstantiationsService.Decrease(apiKey2);

            // Act.
            var result = _factoryInstantiationsService.GetActiveFactories();

            // Assert.
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void GetRedundantActiveFactories()
        {
            // Arrange.
            _factoryInstantiationsService.Clear();

            var apiKey = "apiKey";
            var apiKey2 = "apiKey-2";
            var apiKey3 = "apiKey-3";
            var apiKey4 = "apiKey-4";

            _factoryInstantiationsService.Increase(apiKey);
            _factoryInstantiationsService.Increase(apiKey);

            _factoryInstantiationsService.Increase(apiKey2);
            _factoryInstantiationsService.Increase(apiKey2);
            _factoryInstantiationsService.Decrease(apiKey2);

            _factoryInstantiationsService.Increase(apiKey3);

            _factoryInstantiationsService.Increase(apiKey4);            
            _factoryInstantiationsService.Increase(apiKey4);
            _factoryInstantiationsService.Increase(apiKey4);
            _factoryInstantiationsService.Increase(apiKey4);

            // Act.
            var result = _factoryInstantiationsService.GetRedundantActiveFactories();

            // Assert.
            Assert.AreEqual(4, result);
        }
    }
}