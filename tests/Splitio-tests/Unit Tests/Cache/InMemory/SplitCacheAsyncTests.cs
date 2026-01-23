using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Filters;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class SplitCacheAsyncTests
    {
        private readonly IFlagSetsFilter _flagSetsFilter;
        private readonly IFeatureFlagCache _cache;
        private readonly EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> _eventsManager;
        private readonly IInternalEventsTask _internalEventsTask;
        private bool SdkUpdateFlag = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> SdkUpdate;
        public event EventHandler<EventMetadata> SdkReady;

        public SplitCacheAsyncTests()
        {
            _flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var splits = new ConcurrentDictionary<string, ParsedSplit>();
            _eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig(), new EventDelivery<SdkEvent, EventMetadata>());
            _internalEventsTask = new InternalEventsTask(_eventsManager, new Splitio.Services.Shared.Classes.SplitQueue<Splitio.Services.EventSource.Workers.SdkEventNotification>());
            _internalEventsTask.Start();
            _cache = new InMemorySplitCache(splits, _flagSetsFilter, _internalEventsTask);
        }

        [TestMethod]
        public async Task GetSplitAsyncReturnsNull()
        {
            // Arrange.
            var ffName = "feature-flag";

            // Act.
            var result = await _cache.GetSplitAsync(ffName);

            // Assert.
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetSplitAsyncReturnsObject()
        {
            // Arrange.
            var ffName = "feature-flag";

            var split = new ParsedSplit
            {
                name = ffName,
                defaultTreatment = "on",
                changeNumber = 1
            };

            _cache.Update(new List<ParsedSplit> { split }, new List<string>(), -1);

            // Act.
            var result = await _cache.GetSplitAsync(ffName);

            // Assert.
            Assert.AreEqual(ffName, result.name);
            Assert.AreEqual("on", result.defaultTreatment);
            Assert.AreEqual(1, result.changeNumber);
        }

        [TestMethod]
        public async Task GetAllSplitsAsyncResturnsEmpty()
        {
            // Act.
            var result = await _cache.GetAllSplitsAsync();

            // Assert.
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public async Task GetAllSplitsAsyncResturnsItems()
        {
            // Arrange.
            var split1 = new ParsedSplit
            {
                name = "feature-flag-1",
                defaultTreatment = "on",
                changeNumber = 1
            };

            var split2 = new ParsedSplit
            {
                name = "feature-flag-2",
                defaultTreatment = "on",
                changeNumber = 1
            };

            _cache.Update(new List<ParsedSplit> { split1, split2 }, new List<string>(), -1);

            // Act.
            var result = await _cache.GetAllSplitsAsync();

            // Assert.
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task FetchManyAsyncReturnsEmpty()
        {
            // Act.
            var result = await _cache.FetchManyAsync(new List<string> { "feature-flag-1", "feature-flag-2" });

            // Assert.
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public async Task FetchManyAsyncReturnsItems()
        {
            // Arrange.
            var toAdd = new List<ParsedSplit>();
            for (int i = 1; i <= 3; i++)
            {
                toAdd.Add(new ParsedSplit
                {
                    name = $"feature-flag-{i}",
                    defaultTreatment = "on",
                    changeNumber = i
                });
            }

            _cache.Update(toAdd, new List<string>(), -1);

            // Act.
            var result = await _cache.FetchManyAsync(new List<string> { "feature-flag-2" });

            // Assert.
            Assert.AreEqual(1, result.Count);
            var ff = result.FirstOrDefault();
            Assert.AreEqual("feature-flag-2", ff.name);
            Assert.AreEqual("on", ff.defaultTreatment);
            Assert.AreEqual(2, ff.changeNumber);
        }

        [TestMethod]
        public async Task GetSplitNamesAsyncReturnsEmpty()
        {
            // Act.
            var result = await _cache.GetSplitNamesAsync();

            // Assert.
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public async Task GetSplitNamesAsyncReturnsItems()
        {
            // Arrange.
            var toAdd = new List<ParsedSplit>();
            for (int i = 1; i <= 5; i++)
            {
                toAdd.Add(new ParsedSplit
                {
                    name = $"feature-flag-{i}",
                    defaultTreatment = "on",
                    changeNumber = i
                });
            }

            _cache.Update(toAdd, new List<string>(), -1);

            // Act.
            var result = await _cache.GetSplitNamesAsync();

            // Assert.
            Assert.AreEqual(5, result.Count);
        }

        [TestMethod]
        public async Task NotifyUpdateEventTest()
        {
            // Arrange.
            var toAdd = new List<ParsedSplit>();
            var toNotify = new List<string>();
            for (int i = 1; i <= 5; i++)
            {
                toAdd.Add(new ParsedSplit
                {
                    name = $"feature-flag-{i}",
                    defaultTreatment = "on",
                    changeNumber = i
                });
                toNotify.Add($"feature-flag-{i}");
            }
            SdkUpdate += sdkUpdate_callback;
            _eventsManager.Register(SdkEvent.SdkUpdate, TriggerSdkUpdate);
            _eventsManager.Register(SdkEvent.SdkReady, TriggerSdkReady);
            _eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, null);
           
            SdkUpdateFlag = false;
            _cache.Update(toAdd, new List<string>(), -1);
            SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));

            // Act.
            var result = await _cache.GetSplitNamesAsync();

            // Assert.
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.FlagsUpdate, eMetadata.GetEventType());
            Assert.IsTrue(eMetadata.GetNames().Count == 5);
            for (int i = 1; i <= 5; i++)
            {
                Assert.IsTrue(eMetadata.GetNames().Contains($"feature-flag-{i}"));
            }

            SdkUpdateFlag = false;
            eMetadata = null;
            _cache.Kill(123, "feature-flag-1", "off");
            SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));

            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.FlagsUpdate, eMetadata.GetEventType());
            Assert.IsTrue(eMetadata.GetNames().Count == 1);
            Assert.IsTrue(eMetadata.GetNames().Contains($"feature-flag-1"));
        }

        private void sdkUpdate_callback(object sender, EventMetadata metadata)
        {
            SdkUpdateFlag = true;
            eMetadata = metadata;
        }

        private void TriggerSdkReady(EventMetadata metaData)
        {
            SdkReady?.Invoke(this, metaData);
        }

        private void TriggerSdkUpdate(EventMetadata metaData)
        {
            SdkUpdate?.Invoke(this, metaData);
        }
    }
}
