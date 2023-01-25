using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.EventSource;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Storages;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class PushManagerTests
    {
        private readonly Mock<IAuthApiClient> _authApiClient;
        private readonly Mock<ISSEHandler> _sseHandler;
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;

        private readonly IBackOff _backoff;
        private readonly IPushManager _pushManager;

        public PushManagerTests()
        {
            _authApiClient = new Mock<IAuthApiClient>();
            _sseHandler = new Mock<ISSEHandler>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            var wrapper = WrapperAdapter.Instance();
            _backoff = new BackOff(1, 0);

            _pushManager = new PushManager(_sseHandler.Object, _authApiClient.Object, wrapper, _telemetryRuntimeProducer.Object, _backoff);
        }

        [TestMethod]
        public void StartSse_WithSSEError_ShouldRetry()
        {
            _authApiClient
                .Setup(mock => mock.AuthenticateAsync())
                .ReturnsAsync(new AuthenticationResponse
                {
                    PushEnabled = true,
                    Channels = "channel-test",
                    Token = "token-test",
                    Retry = true,
                    Expiration = 10000000
                });

            _sseHandler
                .SetupSequence(mock => mock.Start(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false)
                .Returns(false)
                .Returns(true);
                
            
            // Act.
            var result = _pushManager.StartSse();
            Thread.Sleep(5000);

            // Assert.
            _authApiClient.Verify(mock => mock.AuthenticateAsync(), Times.Exactly(3));
            _sseHandler.Verify(mock => mock.Start(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
            Assert.AreEqual(0, _backoff.GetAttempt());
            Assert.AreEqual(0, _backoff.GetInterval());
        }

        [TestMethod]
        public void StartSse_WithPushEnabled_ShouldConnect()
        {
            // Arrange.
            var response = new AuthenticationResponse
            {
                PushEnabled = true,
                Channels = "channel-test",
                Token = "token-test",
                Retry = false,
                Expiration = 1
            };

            var response2 = new AuthenticationResponse
            {
                PushEnabled = true,
                Channels = "channel-test-2",
                Token = "token-test-2",
                Retry = false,
                Expiration = 1
            };

            _authApiClient
                .SetupSequence(mock => mock.AuthenticateAsync())
                .ReturnsAsync(response)
                .ReturnsAsync(response2);

            _sseHandler
                .Setup(mock => mock.Start(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            // Act.
            var result = _pushManager.StartSse();

            // Assert.
            Assert.IsTrue(result.Result);
            _authApiClient.Verify(mock => mock.AuthenticateAsync(), Times.Once);
            _sseHandler.Verify(mock => mock.Start(response.Token, response.Channels), Times.Once);

            Thread.Sleep(3000);
            _authApiClient.Verify(mock => mock.AuthenticateAsync(), Times.AtLeast(2));
            _sseHandler.Verify(mock => mock.Start(response2.Token, response2.Channels), Times.Once);
        }

        [TestMethod]
        public void StartSse_WithPushDisable_ShouldNotConnect()
        {
            // Arrange.
            var response = new AuthenticationResponse
            {
                PushEnabled = false,
                Retry = false
            };

            _authApiClient
                .Setup(mock => mock.AuthenticateAsync())
                .ReturnsAsync(response);

            // Act.
            var result = _pushManager.StartSse();

            // Assert.
            Assert.IsFalse(result.Result);
            _authApiClient.Verify(mock => mock.AuthenticateAsync(), Times.Once);
            _sseHandler.Verify(mock => mock.Start(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _sseHandler.Verify(mock => mock.Stop(), Times.Once);

            Thread.Sleep(5000);
            _authApiClient.Verify(mock => mock.AuthenticateAsync(), Times.Once);
            _sseHandler.Verify(mock => mock.Start(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _sseHandler.Verify(mock => mock.Stop(), Times.Once);
        }

        [TestMethod]
        public void StartSse_WithPushDisableAndRetryTrue_ShouldNotConnect()
        {
            // Arrange.
            var response = new AuthenticationResponse
            {
                PushEnabled = false,
                Retry = true
            };

            var response2 = new AuthenticationResponse
            {
                PushEnabled = true,
                Channels = "channel-test-2",
                Token = "token-test-2",
                Retry = false,
                Expiration = 1
            };

            _authApiClient
                .SetupSequence(mock => mock.AuthenticateAsync())
                .ReturnsAsync(response)
                .ReturnsAsync(response2);

            // Act.
            var result = _pushManager.StartSse();

            // Assert.
            Assert.IsFalse(result.Result);
            _authApiClient.Verify(mock => mock.AuthenticateAsync(), Times.Once);
            _sseHandler.Verify(mock => mock.Start(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _sseHandler.Verify(mock => mock.Stop(), Times.Once);

            Thread.Sleep(3500);
            _authApiClient.Verify(mock => mock.AuthenticateAsync(), Times.AtLeast(2));
            _sseHandler.Verify(mock => mock.Start(response2.Token, response2.Channels), Times.Once);
        }
    }
}
