using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Client.Classes;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Tasks;
using Splitio_Tests.Resources;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class ImpressionsCounterTests
    {
        private readonly Mock<IImpressionsSenderAdapter> _senderAdapter;

        public ImpressionsCounterTests()
        {
            _senderAdapter = new Mock<IImpressionsSenderAdapter>();
        }

        [TestMethod]
        public void Start_ShouldSendImpressionsCount()
        {
            // Arrange.
            var config = new ComponentConfig(5, 5);
            var taskManager = new TasksManager();
            var statusManager = new InMemoryReadinessGatesCache();
            var task = taskManager.NewPeriodicTask(statusManager, Splitio.Enums.Task.ImpressionsCountSender, 1);
            var impressionsCounter = new ImpressionsCounter(config, _senderAdapter.Object, task);


            // Act.
            impressionsCounter.Inc("feature1", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 15, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature1", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature2", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature3", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 10, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Start();

            // Assert.
            Thread.Sleep(1500);
            _senderAdapter.Verify(mock => mock.RecordImpressionsCount(It.IsAny<List<ImpressionsCountModel>>()), Times.Once);
        }

        [TestMethod]
        public void Start_ShouldNotSendImpressionsCount()
        {
            // Arrange.
            var config = new ComponentConfig(5, 5);
            var taskManager = new TasksManager();
            var statusManager = new InMemoryReadinessGatesCache();
            var task = taskManager.NewPeriodicTask(statusManager, Splitio.Enums.Task.ImpressionsCountSender, 1);
            var impressionsCounter = new ImpressionsCounter(config, _senderAdapter.Object, task);

            // Act.
            impressionsCounter.Start();

            // Assert.
            Thread.Sleep(1500);
            _senderAdapter.Verify(mock => mock.RecordImpressionsCount(It.IsAny<List<ImpressionsCountModel>>()), Times.Never);
        }

        [TestMethod]
        public void Stop_ShouldSendImpressionsCount()
        {
            // Arrange.
            var config = new ComponentConfig(5, 5);
            var taskManager = new TasksManager();
            var statusManager = new InMemoryReadinessGatesCache();
            var task = taskManager.NewPeriodicTask(statusManager, Splitio.Enums.Task.ImpressionsCountSender, 100);
            var impressionsCounter = new ImpressionsCounter(config, _senderAdapter.Object, task);

            impressionsCounter.Inc("feature1", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 15, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature1", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature2", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature3", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 10, 50, 11, DateTimeKind.Utc)));

            // Act.
            impressionsCounter.Start();
            Thread.Sleep(1000);
            impressionsCounter.Stop();

            // Assert.
            _senderAdapter.Verify(mock => mock.RecordImpressionsCount(It.IsAny<List<ImpressionsCountModel>>()), Times.Once);
        }

        [TestMethod]
        public void ShouldSend2BulksOfImpressions()
        {
            var config = new ComponentConfig(6, 3);
            var taskManager = new TasksManager();
            var statusManager = new InMemoryReadinessGatesCache();
            var task = taskManager.NewPeriodicTask(statusManager, Splitio.Enums.Task.ImpressionsCountSender, 100);
            var impressionsCounter = new ImpressionsCounter(config, _senderAdapter.Object, task);

            impressionsCounter.Inc("feature1", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 15, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature1", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature2", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature3", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 10, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature3", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 15, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature4", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));
            impressionsCounter.Inc("feature6", SplitsHelper.MakeTimestamp(new DateTime(2020, 09, 02, 09, 50, 11, DateTimeKind.Utc)));

            // Act.
            impressionsCounter.Start();
            Thread.Sleep(1000);
            impressionsCounter.Stop();

            // Assert.
            _senderAdapter.Verify(mock => mock.RecordImpressionsCount(It.IsAny<List<ImpressionsCountModel>>()), Times.Exactly(2));
        }
    }
}
