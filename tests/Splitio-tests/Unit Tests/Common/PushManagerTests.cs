using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.EventSource;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Storages;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Common
{
    [TestClass]
    public class PushManagerTests
    {
        private readonly Mock<IAuthApiClient> _authApiClient;
        private readonly Mock<ISSEHandler> _sseHandler;
        private readonly Mock<ITelemetryRuntimeProducer> _telemetryRuntimeProducer;
        private readonly Mock<INotificationManagerKeeper> _notificationManagerKeeper;
        private readonly Mock<ISplitTask> _refreshTokenTask;
        private readonly Mock<IStatusManager> _statusManager;

        private readonly IPushManager _pushManager;

        public PushManagerTests()
        {
            _authApiClient = new Mock<IAuthApiClient>();
            _sseHandler = new Mock<ISSEHandler>();
            _telemetryRuntimeProducer = new Mock<ITelemetryRuntimeProducer>();
            _notificationManagerKeeper = new Mock<INotificationManagerKeeper>();
            _refreshTokenTask = new Mock<ISplitTask>();
            _statusManager = new Mock<IStatusManager>();

            _pushManager = new PushManager(_sseHandler.Object, _authApiClient.Object, _telemetryRuntimeProducer.Object, _notificationManagerKeeper.Object, _refreshTokenTask.Object, _statusManager.Object);
        }

        [TestMethod]
        public async Task Start_WithPushEnabled_ShouldConnect()
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
                .ReturnsAsync(response);

            // Act.
            await _pushManager.StartAsync();

            // Assert.
            _authApiClient.Verify(mock => mock.AuthenticateAsync(), Times.Once);
            _sseHandler.Verify(mock => mock.Start(response.Token, response.Channels), Times.Once);
            _notificationManagerKeeper.Verify(mock => mock.HandleSseStatus(It.IsAny<SSEClientStatusMessage>()), Times.Never);
        }

        [TestMethod]
        public async Task Start_WithPushDisable_ShouldNotConnect()
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
            await _pushManager.StartAsync();

            // Assert.
            _authApiClient.Verify(mock => mock.AuthenticateAsync(), Times.Once);
            _sseHandler.Verify(mock => mock.Start(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _notificationManagerKeeper.Verify(mock => mock.HandleSseStatus(SSEClientStatusMessage.FORCED_STOP), Times.Once);
        }

        [TestMethod]
        public async Task Start_WithPushDisableAndRetryTrue_ShouldNotConnect()
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
            await _pushManager.StartAsync();

            // Assert.
            _authApiClient.Verify(mock => mock.AuthenticateAsync(), Times.Once);
            _sseHandler.Verify(mock => mock.Start(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _notificationManagerKeeper.Verify(mock => mock.HandleSseStatus(SSEClientStatusMessage.RETRYABLE_ERROR), Times.Once);
        }
    }
}
