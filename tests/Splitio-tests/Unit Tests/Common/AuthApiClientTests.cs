﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.CommonLibraries;
using Splitio.Services.Common;
using Splitio.Telemetry.Storages;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class AuthApiClientTests
    {
        private readonly Mock<ISplitioHttpClient> _splitioHttpClientMock;
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly IAuthApiClient _authApiClient;

        public AuthApiClientTests()
        {
            _splitioHttpClientMock = new Mock<ISplitioHttpClient>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();

            _authApiClient = new AuthApiClient("https://auth.fake.io/auth", _splitioHttpClientMock.Object, _telemetryRuntimeProducer.Object);
        }

        [TestMethod]
        public async Task AuthenticateAsync_WithPushEnabled_ShouldReturnSuccessResponse()
        {
            //Arrange
            var authResponse = "{\"pushEnabled\":true,\"token\":\"khdkjdahs987498217.eyJ4LWFibHktY2FwYWJpbGl0eSI6IntcInh4eHhfeHh4eF9zZWdtZW50c1wiOltcInN1YnNjcmliZVwiXSxcInh4eHhfeHh4eF9zcGxpdHNcIjpbXCJzdWJzY3JpYmVcIl0sXCJjb250cm9sXCI6W1wic3Vic2NyaWJlXCJdfSJ9\"}";

            _splitioHttpClientMock
                .Setup(mock => mock.GetAsync("https://auth.fake.io/auth?s=1.3", false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = authResponse,
                    IsSuccessStatusCode = true
                });

            //Act
            var result = await _authApiClient.AuthenticateAsync();

            //Assert
            Assert.IsTrue(result.PushEnabled.Value);
            Assert.AreEqual("xxxx_xxxx_segments,xxxx_xxxx_splits,control", result.Channels);
            Assert.IsFalse(string.IsNullOrEmpty(result.Token));
            Assert.IsTrue(result.Retry.Value);
            _splitioHttpClientMock.Verify(mock => mock.GetAsync("https://auth.fake.io/auth?s=1.3", false), Times.Once);
        }

        [TestMethod]
        public async Task AuthenticateAsync_WithPushDisabled_ShouldReturnSuccessResponse()
        {
            //Arrange
            var authResponse = "{\"pushEnabled\":false}";

            _splitioHttpClientMock
                .Setup(mock => mock.GetAsync("https://auth.fake.io/auth?s=1.3", false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = authResponse,
                    IsSuccessStatusCode = true
                });

            //Act
            var result = await _authApiClient.AuthenticateAsync();

            //Assert
            Assert.IsFalse(result.PushEnabled.Value);
            Assert.IsTrue(string.IsNullOrEmpty(result.Channels));
            Assert.IsFalse(result.Retry.Value);
            Assert.IsTrue(string.IsNullOrEmpty(result.Token));
            _splitioHttpClientMock.Verify(mock => mock.GetAsync("https://auth.fake.io/auth?s=1.3", false), Times.Once);
        }

        [TestMethod]
        public async Task AuthenticateAsync_ServerReturnError_ShouldReturnRetry_True()
        {
            //Arrange
            _splitioHttpClientMock
                .Setup(mock => mock.GetAsync("https://auth.fake.io/auth?s=1.3", false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.InternalServerError,
                    IsSuccessStatusCode = false
                });

            //Act
            var result = await _authApiClient.AuthenticateAsync();

            //Assert
            Assert.IsFalse(result.PushEnabled.Value);
            Assert.IsTrue(string.IsNullOrEmpty(result.Channels));
            Assert.IsTrue(result.Retry.Value);
            Assert.IsTrue(string.IsNullOrEmpty(result.Token));
            _splitioHttpClientMock.Verify(mock => mock.GetAsync("https://auth.fake.io/auth?s=1.3", false), Times.Once);
        }

        [TestMethod]
        public async Task AuthenticateAsync_ServerReturnBadRequest_ShouldReturnRetry_False()
        {
            //Arrange
            _splitioHttpClientMock
                .Setup(mock => mock.GetAsync("https://auth.fake.io/auth?s=1.3", false))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    IsSuccessStatusCode = false
                });

            //Act
            var result = await _authApiClient.AuthenticateAsync();

            //Assert
            Assert.IsFalse(result.Retry.Value);
            Assert.IsFalse(result.PushEnabled.Value);
            Assert.IsTrue(string.IsNullOrEmpty(result.Channels));            
            Assert.IsTrue(string.IsNullOrEmpty(result.Token));
            _splitioHttpClientMock.Verify(mock => mock.GetAsync("https://auth.fake.io/auth?s=1.3", false), Times.Once);
        }

        [TestMethod]
        public async Task AuthenticateAsync_ServerReturnUnauthorized_ShouldReturnRetry_False()
        {
            //Arrange
            _splitioHttpClientMock
                .Setup(mock => mock.GetAsync("https://auth.fake.io/auth?s=1.3", It.IsAny<bool>()))
                .ReturnsAsync(new HTTPResult
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized,
                });

            //Act
            var result = await _authApiClient.AuthenticateAsync();

            //Assert
            Assert.IsFalse(result.Retry.Value);
            Assert.IsFalse(result.PushEnabled.Value);
            Assert.IsTrue(string.IsNullOrEmpty(result.Channels));
            Assert.IsTrue(string.IsNullOrEmpty(result.Token));
            _splitioHttpClientMock.Verify(mock => mock.GetAsync("https://auth.fake.io/auth?s=1.3", false), Times.Once);
        }
    }
}
