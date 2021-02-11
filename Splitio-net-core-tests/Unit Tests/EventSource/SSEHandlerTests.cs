﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.EventSource;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.Logger;

namespace Splitio_Tests.Unit_Tests.EventSource
{
    [TestClass]
    public class SSEHandlerTests
    {
        private readonly Mock<ISplitLogger> _log;
        private readonly Mock<ISplitsWorker> _splitsWorker;
        private readonly Mock<ISegmentsWorker> _segmentsWorker;
        private readonly Mock<INotificationProcessor> _notificationPorcessor;
        private readonly Mock<IEventSourceClient> _eventSourceClient;
        private readonly Mock<INotificationManagerKeeper> _notificationManagerKeeper;
        private readonly ISSEHandler _sseHandler;

        public SSEHandlerTests()
        {
            _log = new Mock<ISplitLogger>();
            _splitsWorker = new Mock<ISplitsWorker>();
            _segmentsWorker = new Mock<ISegmentsWorker>();
            _notificationPorcessor = new Mock<INotificationProcessor>();
            _eventSourceClient = new Mock<IEventSourceClient>();
            _notificationManagerKeeper = new Mock<INotificationManagerKeeper>();

            _sseHandler = new SSEHandler("www.fake.com", _splitsWorker.Object, _segmentsWorker.Object, _notificationPorcessor.Object, _notificationManagerKeeper.Object, _log.Object, _eventSourceClient.Object);
        }

        [TestMethod]
        public void Start_ShouldConnect()
        {
            // Arrange.
            var token = "fake-test";
            var channels = "channel-test";

            _eventSourceClient
                .Raise(mock => mock.ConnectedEvent += null, new FeedbackEventArgs(true));

            // Act.
            _sseHandler.Start(token, channels);

            // Assert.
            _eventSourceClient.Verify(mock => mock.ConnectAsync(It.IsAny<string>()), Times.Once);
            _splitsWorker.Verify(mock => mock.Start(), Times.Once);
            _segmentsWorker.Verify(mock => mock.Start(), Times.Once);
        }

        [TestMethod]
        public void Stop_ShouldDisconnect()
        {
            // Arrange.
            var token = "fake-test";
            var channels = "channel-test";

            _eventSourceClient
                .Setup(mock => mock.Disconnect(It.IsAny<bool>()))
                .Raises(mock => mock.DisconnectEvent += null, new FeedbackEventArgs(false));

            // Act.
            _sseHandler.Start(token, channels);
            _sseHandler.Stop();

            // Assert.
            _eventSourceClient.Verify(mock => mock.Disconnect(It.IsAny<bool>()), Times.Once);
            _splitsWorker.Verify(mock => mock.Stop(), Times.Once);
            _segmentsWorker.Verify(mock => mock.Stop(), Times.Once);
        }
    }
}
