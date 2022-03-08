using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio_Tests.Resources;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class ImpressionsCountSenderTests
    {
        private readonly WrapperAdapter wrapperAdapter = new WrapperAdapter();

        private readonly Mock<IImpressionsSenderAdapter> _senderAdapter;

        public ImpressionsCountSenderTests()
        {
            _senderAdapter = new Mock<IImpressionsSenderAdapter>();
        }

        [TestMethod]
        public void Start_ShouldSendImpressionsCount()
        {
            // Arrange.
            var impressionsCounter = new ImpressionsCounter();
            impressionsCounter.Inc("feature1", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 15, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature1", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature2", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature3", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 10, 50, 11, DateTimeKind.Utc)));

            var impressionsCountSender = new ImpressionsCountSender(_senderAdapter.Object, impressionsCounter, new TasksManager(wrapperAdapter), 1);

            // Act.
            impressionsCountSender.Start();

            // Assert.
            Thread.Sleep(1500);
            _senderAdapter.Verify(mock => mock.RecordImpressionsCount(It.IsAny<ConcurrentDictionary<KeyCache, int>>()), Times.Once);
        }

        [TestMethod]
        public void Start_ShouldNotSendImpressionsCount()
        {
            // Arrange.
            var impressionsCounter = new ImpressionsCounter();
            var impressionsCountSender = new ImpressionsCountSender(_senderAdapter.Object, impressionsCounter, new TasksManager(wrapperAdapter), 1);

            // Act.
            impressionsCountSender.Start();

            // Assert.
            Thread.Sleep(1500);
            _senderAdapter.Verify(mock => mock.RecordImpressionsCount(It.IsAny<ConcurrentDictionary<KeyCache, int>>()), Times.Never);
        }

        [TestMethod]
        public void Stop_ShouldSendImpressionsCount()
        {
            // Arrange.
            var impressionsCounter = new ImpressionsCounter();
            impressionsCounter.Inc("feature1", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 15, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature1", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature2", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature3", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 10, 50, 11, DateTimeKind.Utc)));

            var impressionsCountSender = new ImpressionsCountSender(_senderAdapter.Object, impressionsCounter, new TasksManager(wrapperAdapter));

            // Act.
            impressionsCountSender.Start();
            Thread.Sleep(1000);
            impressionsCountSender.Stop();

            // Assert.
            _senderAdapter.Verify(mock => mock.RecordImpressionsCount(It.IsAny<ConcurrentDictionary<KeyCache, int>>()), Times.Once);
        }
    }
}
