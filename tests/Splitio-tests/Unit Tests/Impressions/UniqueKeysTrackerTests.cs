using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Filter;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Domain;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class UniqueKeysTrackerTests
    {
        private readonly Mock<IFilterAdapter> _filterAdapter;
        private readonly Mock<IImpressionsSenderAdapter> _senderAdapter;
        private readonly Mock<IStatusManager> _statusManager;
        private readonly ITasksManager _tasksManager;
        private readonly ConcurrentDictionary<string, HashSet<string>> _cache;

        private IUniqueKeysTracker _uniqueKeysTracker;

        public UniqueKeysTrackerTests()
        {
            _filterAdapter = new Mock<IFilterAdapter>();
            _senderAdapter = new Mock<IImpressionsSenderAdapter>();
            _statusManager = new Mock<IStatusManager>();
            _tasksManager = new TasksManager(_statusManager.Object);
            _cache = new ConcurrentDictionary<string, HashSet<string>>();
        }

        [TestMethod]
        public async Task PeriodicTask_ShouldSendBulk()
        {
            // Arrange.
            _cache.Clear();

            var config = new ComponentConfig(5, 5);
            var task = _tasksManager.NewPeriodicTask(Splitio.Enums.Task.MTKsSender, 1);
            var cacheLongTermCleaningTask = _tasksManager.NewPeriodicTask(Splitio.Enums.Task.CacheLongTermCleaning, 3600);
            var sendBulkDataTask = _tasksManager.NewOnTimeTask(Splitio.Enums.Task.MtkSendBulkData);
            _uniqueKeysTracker = new UniqueKeysTracker(config, _filterAdapter.Object, _cache, _senderAdapter.Object, task, cacheLongTermCleaningTask, sendBulkDataTask);

            // Act.
            _uniqueKeysTracker.Start();

            // Assert.
            Assert.IsTrue(_uniqueKeysTracker.Track("key-test", "feature-name-test"));

            Thread.Sleep(2000);

            _senderAdapter.Verify(mock => mock.RecordUniqueKeysAsync(It.IsAny<List<Mtks>>()), Times.Once);

            Assert.IsTrue(_uniqueKeysTracker.Track("key-test", "feature-name-test"));
            await _uniqueKeysTracker.StopAsync();

            _senderAdapter.Verify(mock => mock.RecordUniqueKeysAsync(It.IsAny<List<Mtks>>()), Times.Exactly(2));

            _cache.Clear();
        }

        [TestMethod]
        public void Track_WithFullSize_ShouldSendTwoBulk()
        {
            // Arrange.
            _cache.Clear();

            var config = new ComponentConfig(5, 5);
            var task = _tasksManager.NewPeriodicTask(Splitio.Enums.Task.MTKsSender, 1);
            var cacheLongTermCleaningTask = _tasksManager.NewPeriodicTask(Splitio.Enums.Task.CacheLongTermCleaning, 3600);
            var sendBulkDataTask = _tasksManager.NewOnTimeTask(Splitio.Enums.Task.MtkSendBulkData);
            _uniqueKeysTracker = new UniqueKeysTracker(config, _filterAdapter.Object, _cache, _senderAdapter.Object, task, cacheLongTermCleaningTask, sendBulkDataTask);

            _filterAdapter
                .SetupSequence(mock => mock.Contains("feature-name-test", "key-test"))
                .Returns(false)
                .Returns(true);

            _filterAdapter
                .SetupSequence(mock => mock.Contains("feature-name-test-2", "key-test-2"))
                .Returns(false)
                .Returns(true);

            // Act && Assert.
            Assert.IsTrue(_uniqueKeysTracker.Track("key-test", "feature-name-test"));
            Assert.IsFalse(_uniqueKeysTracker.Track("key-test", "feature-name-test"));
            Assert.IsTrue(_uniqueKeysTracker.Track("key-test-2", "feature-name-test"));
            Assert.IsTrue(_uniqueKeysTracker.Track("key-test-2", "feature-name-test-2"));
            Thread.Sleep(2000);
            Assert.IsFalse(_uniqueKeysTracker.Track("key-test-2", "feature-name-test-2"));
            Assert.IsTrue(_uniqueKeysTracker.Track("key-test-2", "feature-name-test-3"));
            Assert.IsTrue(_uniqueKeysTracker.Track("key-test-2", "feature-name-test-4"));
            Assert.AreEqual(4, _cache.Count);
            _cache.TryGetValue("feature-name-test", out HashSet<string> values);
            Assert.AreEqual(2, values.Count);
            _cache.TryGetValue("feature-name-test-2", out HashSet<string> values2);
            Assert.AreEqual(1, values2.Count);
            _cache.TryGetValue("feature-name-test-3", out HashSet<string> values3);
            Assert.AreEqual(1, values3.Count);
            Assert.IsTrue(_uniqueKeysTracker.Track("key-test", "feature-name-test-5"));
            Assert.IsTrue(_uniqueKeysTracker.Track("key-test-2", "feature-name-test-5"));

            Thread.Sleep(2000);
            _senderAdapter.Verify(mock => mock.RecordUniqueKeysAsync(It.IsAny<List<Mtks>>()), Times.Exactly(2));

            _cache.Clear();
        }

        [TestMethod]
        public void Track_WithFullSize_ShouldSplitBulks()
        {
            // Arrange.
            Mock<IFilterAdapter> _filterAdapter2 = new Mock<IFilterAdapter>();
            Mock<IStatusManager> _statusManager2 = new Mock<IStatusManager>();
            TasksManager _tasksManager2 = new TasksManager(_statusManager2.Object);
            var _cache2 = new ConcurrentDictionary<string, HashSet<string>>();
            _cache2.Clear();
            Mock<IImpressionsSenderAdapter> _senderAdapter2 = new Mock<IImpressionsSenderAdapter>();
                        
            var config = new ComponentConfig(6, 3);
            var task = _tasksManager2.NewPeriodicTask(Splitio.Enums.Task.MTKsSender, 1);
            var cacheLongTermCleaningTask = _tasksManager2.NewPeriodicTask(Splitio.Enums.Task.CacheLongTermCleaning, 3600);
            var sendBulkDataTask = _tasksManager2.NewOnTimeTask(Splitio.Enums.Task.MtkSendBulkData);
            UniqueKeysTracker _uniqueKeysTracker2 = new UniqueKeysTracker(config, _filterAdapter2.Object, _cache2, _senderAdapter2.Object, task, cacheLongTermCleaningTask, sendBulkDataTask);

            // Act && Assert.
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-1", "feature-name-test"));
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-2", "feature-name-test"));
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-3", "feature-name-test"));
            Thread.Sleep(1000);
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-4", "feature-name-test"));
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-5", "feature-name-test"));
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-6", "feature-name-test"));
            Thread.Sleep(1000);
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-1", "feature-name-test-2"));
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-2", "feature-name-test-2"));
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-3", "feature-name-test-2"));
            Thread.Sleep(1000);
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-4", "feature-name-test-2"));
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-5", "feature-name-test-2"));
            Assert.IsTrue(_uniqueKeysTracker2.Track("key-test-6", "feature-name-test-2"));
            Thread.Sleep(1000);

            _senderAdapter2.Verify(mock => mock.RecordUniqueKeysAsync(It.IsAny<List<Mtks>>()), Times.Exactly(4));

            _cache2.Clear();
        }
    }
}
