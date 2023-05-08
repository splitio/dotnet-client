using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Common;
using Splitio.Services.EventSource.Workers;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.EventSource.Workers
{
    [TestClass]
    public class SplitsWorkerTests
    {
        private readonly IWrapperAdapter wrapperAdapter = WrapperAdapter.Instance();

        private readonly Mock<ISynchronizer> _synchronizer;

        private readonly ISplitsWorker _splitsWorker;

        public SplitsWorkerTests()
        {
            _synchronizer = new Mock<ISynchronizer>();

            _splitsWorker = new SplitsWorker(_synchronizer.Object, new TasksManager(wrapperAdapter));
        }

        [TestMethod]        
        public void AddToQueue_WithElements_ShouldTriggerFetch()
        {
            // Act.
            _splitsWorker.Start();
            _splitsWorker.AddToQueue(1585956698457);
            _splitsWorker.AddToQueue(1585956698467);
            _splitsWorker.AddToQueue(1585956698477);
            _splitsWorker.AddToQueue(1585956698476);
            Thread.Sleep(1000);

            _splitsWorker.Stop();
            _splitsWorker.AddToQueue(1585956698486);
            Thread.Sleep(100);
            _splitsWorker.AddToQueue(1585956698496);

            // Assert
            _synchronizer.Verify(mock => mock.SynchronizeSplits(It.IsAny<long>()), Times.Exactly(4));
        }

        [TestMethod]
        public void AddToQueue_WithoutElemts_ShouldNotTriggerFetch()
        {
            // Act.
            _splitsWorker.Start();
            Thread.Sleep(500);

            // Assert.
            _synchronizer.Verify(mock => mock.SynchronizeSplits(It.IsAny<long>()), Times.Never);
        }
    }
}
