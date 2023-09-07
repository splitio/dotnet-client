using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Tasks
{
    [TestClass]
    public class TasksManagerTests
    {
        private readonly Mock<IStatusManager> _statusManager;
        private readonly ITasksManager _taskManager;

        public TasksManagerTests()
        {
            _statusManager = new Mock<IStatusManager>();

            _taskManager = new TasksManager(_statusManager.Object);
        }

        [TestMethod]
        public void NewOnTimeTaskAndStartSync()
        {
            // Arrange.
            var count = 0;

            // Act.
            var task = _taskManager.NewOnTimeTaskAndStart(Splitio.Enums.Task.Track, () => count++);
            Thread.Sleep(100);

            // Assert.
            Assert.AreEqual(1, count);
            Assert.IsFalse(task.IsRunning());
        }

        [TestMethod]
        public void NewOnTimeTaskAndStartAsync()
        {
            // Arrange.
            var count = 0;

            // Act.
            var task = _taskManager.NewOnTimeTaskAndStart(Splitio.Enums.Task.Track, async () =>
            {
                await Task.Delay(1);
                count++;
            });
            Thread.Sleep(100);

            // Assert.
            Assert.AreEqual(1, count);
            Assert.IsFalse(task.IsRunning());
        }

        [TestMethod]
        public void NewOnTimeTaskAndStartSyncWhenIsDestroyed()
        {
            // Arrange.
            var count = 0;

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(true);

            // Act.
            var task = _taskManager.NewOnTimeTaskAndStart(Splitio.Enums.Task.Track, () => count++);
            Thread.Sleep(100);

            // Assert.
            Assert.AreEqual(0, count);
            Assert.IsFalse(task.IsRunning());
        }

        [TestMethod]
        public void NewOnTimeTaskAndStartAsyncWhenIsDestroyed()
        {
            // Arrange.
            var count = 0;

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(true);

            // Act.
            var task = _taskManager.NewOnTimeTaskAndStart(Splitio.Enums.Task.Track, async () =>
            {
                await Task.Delay(1);
                count++;
            });
            Thread.Sleep(100);

            // Assert.
            Assert.AreEqual(0, count);
            Assert.IsFalse(task.IsRunning());
        }

        [TestMethod]
        public void NewOnTimeTask()
        {
            // Arrange.
            var count = 0;

            // Act & Assert.
            var task = _taskManager.NewOnTimeTask(Splitio.Enums.Task.Track);
            task.SetAction(() => count++);
            Assert.IsFalse(task.IsRunning());

            task.Start();
            Thread.Sleep(100);

            Assert.IsFalse(task.IsRunning());
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void NewOnTimeTaskWithoutAction()
        {
            // Arrange.
            var count = 0;

            // Act & Assert.
            var task = _taskManager.NewOnTimeTask(Splitio.Enums.Task.Track);
            Assert.IsFalse(task.IsRunning());
            
            task.Start();
            Thread.Sleep(100);

            Assert.IsFalse(task.IsRunning());
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void NewOnTimeTaskWhenIsDestroyed()
        {
            // Arrange.
            var count = 0;

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(true);

            // Act & Assert.
            var task = _taskManager.NewOnTimeTask(Splitio.Enums.Task.Track);
            Assert.IsFalse(task.IsRunning());

            task.Start();
            Thread.Sleep(100);

            Assert.IsFalse(task.IsRunning());
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void NewScheduledTask()
        {
            // Arrange.
            var count = 0;

            // Act & Assert.
            var task = _taskManager.NewScheduledTask(Splitio.Enums.Task.Track, 2);
            task.SetAction(() => count++);
            Assert.IsFalse(task.IsRunning());

            task.Start();
            Thread.Sleep(100);

            Assert.IsFalse(task.IsRunning());
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void NewScheduledTaskWithoutAction()
        {
            // Arrange.
            var count = 0;

            // Act & Assert.
            var task = _taskManager.NewScheduledTask(Splitio.Enums.Task.Track, 2);
            Assert.IsFalse(task.IsRunning());

            task.Start();
            Thread.Sleep(100);

            Assert.IsFalse(task.IsRunning());
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void NewScheduledTaskWhenIsDestroyed()
        {
            // Arrange.
            var count = 0;

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(true);

            // Act & Assert.
            var task = _taskManager.NewScheduledTask(Splitio.Enums.Task.Track, 2);
            task.SetAction(() => count++);
            Assert.IsFalse(task.IsRunning());

            task.Start();
            Thread.Sleep(100);

            Assert.IsFalse(task.IsRunning());
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task NewPeriodicTask()
        {
            // Arrange.
            var count = 0;
            var onStop = 0;

            // Act & Assert.
            var task = _taskManager.NewPeriodicTask(Splitio.Enums.Task.Track, 2);
            task.SetAction(() => count++);
            task.OnStop(() => onStop++);

            Assert.IsFalse(task.IsRunning());

            task.Start();
            Thread.Sleep(100);

            Assert.IsTrue(task.IsRunning());
            Assert.IsTrue(count > 0);
            Assert.AreEqual(0, onStop);

            await task.StopAsync();
            Assert.IsFalse(task.IsRunning());
            Assert.AreEqual(1, onStop);
        }

        [TestMethod]
        public void NewPeriodicTaskWithoutAction()
        {
            // Arrange.
            var count = 0;

            // Act & Assert.
            var task = _taskManager.NewPeriodicTask(Splitio.Enums.Task.Track, 2);
            Assert.IsFalse(task.IsRunning());

            task.Start();
            Thread.Sleep(100);

            Assert.IsFalse(task.IsRunning());
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void NewPeriodicTaskWhenIsDestroyed()
        {
            // Arrange.
            var count = 0;

            _statusManager
                .Setup(mock => mock.IsDestroyed())
                .Returns(true);

            // Act & Assert.
            var task = _taskManager.NewPeriodicTask(Splitio.Enums.Task.Track, 2);
            task.SetAction(() => count++);

            Assert.IsFalse(task.IsRunning());

            task.Start();
            Thread.Sleep(100);

            Assert.IsFalse(task.IsRunning());
            Assert.AreEqual(0, count);
        }
    }
}
