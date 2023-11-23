using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.EventSource.Workers
{
    [TestClass]
    public class SegmentsWorkerTests
    {
        private readonly Mock<ISynchronizer> _synchronizer;
        private readonly Mock<IStatusManager> _statusManager;

        private readonly ISegmentsWorker _segmentsWorker;

        public SegmentsWorkerTests()
        {
            _synchronizer = new Mock<ISynchronizer>();
            _statusManager = new Mock<IStatusManager>();

            var tasksManager = new TasksManager(_statusManager.Object);
            var task = tasksManager.NewPeriodicTask(Splitio.Enums.Task.SegmentsWorker, 0);
            
            _segmentsWorker = new SegmentsWorker(_synchronizer.Object,task);
        }

        [TestMethod]
        public async Task AddToQueue_WithElements_ShouldTriggerFetch()
        {
            // Arrange.
            var changeNumber = 1585956698457;
            var segmentName = "segment-test";

            var changeNumber2 = 1585956698467;
            var segmentName2 = "segment-test-2";

            var changeNumber3 = 1585956698477;
            var segmentName3 = "segment-test-3";

            _segmentsWorker.Start();

            // Act.
            _segmentsWorker.AddToQueue(changeNumber, segmentName);
            _segmentsWorker.AddToQueue(changeNumber2, segmentName2);
            _segmentsWorker.AddToQueue(changeNumber3, segmentName3);
            Thread.Sleep(1000);

            await _segmentsWorker.StopAsync();
            _segmentsWorker.AddToQueue(1585956698487, "segment-test-4");
            Thread.Sleep(10);

            // Assert.
            _synchronizer.Verify(mock => mock.SynchronizeSegmentAsync(It.IsAny<string>(), It.IsAny<long>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AddToQueue_WithoutElemts_ShouldNotTriggerFetch()
        {
            // Act.
            _segmentsWorker.Start();
            Thread.Sleep(500);

            // Assert.
            _synchronizer.Verify(mock => mock.SynchronizeSegmentAsync(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
        }
    }
}
