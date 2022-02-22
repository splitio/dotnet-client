﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Services.Cache.Filter;
using Splitio.Services.Impressions.Classes;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Domain;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class UniqueKeysTrackerTests
    {
        private readonly Mock<IFilterAdapter> _filterAdapter;
        private readonly Mock<ITelemetryAPI> _telemetryApi;
        private readonly ITasksManager _tasksManager;
        private readonly ConcurrentDictionary<string, HashSet<string>> _cache;

        private IUniqueKeysTracker _uniqueKeysTracker;

        public UniqueKeysTrackerTests()
        {
            _filterAdapter = new Mock<IFilterAdapter>();
            _telemetryApi = new Mock<ITelemetryAPI>();
            _tasksManager = new TasksManager(new WrapperAdapter());
            _cache = new ConcurrentDictionary<string, HashSet<string>>();
        }

        [TestMethod]
        public void PeriodicTask_ShouldSendBulk()
        {
            // Arrange.
            _cache.Clear();

            _uniqueKeysTracker = new UniqueKeysTracker(_filterAdapter.Object, _cache, 5, _telemetryApi.Object, _tasksManager, 1);

            // Act.
            _uniqueKeysTracker.Start();

            // Assert.
            Assert.IsTrue(_uniqueKeysTracker.Track("key-test", "feature-name-test"));

            Thread.Sleep(1500);

            _telemetryApi.Verify(mock => mock.RecordUniqueKeys(It.IsAny<UniqueKeys>()), Times.Once);

            Assert.IsTrue(_uniqueKeysTracker.Track("key-test", "feature-name-test"));
            _uniqueKeysTracker.Stop();

            _telemetryApi.Verify(mock => mock.RecordUniqueKeys(It.IsAny<UniqueKeys>()), Times.Exactly(2));

            _cache.Clear();
        }

        [TestMethod]
        public void Track_WithFullSize_ShouldSendBulk()
        {
            // Arrange.
            _cache.Clear();

            _uniqueKeysTracker = new UniqueKeysTracker(_filterAdapter.Object, _cache, 5, _telemetryApi.Object, _tasksManager, 1);

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
            Assert.IsTrue(_uniqueKeysTracker.Track("key-test-2", "feature-name-test-5"));

            _telemetryApi.Verify(mock => mock.RecordUniqueKeys(It.IsAny<UniqueKeys>()), Times.Once);

            _cache.Clear();
        }
    }
}
