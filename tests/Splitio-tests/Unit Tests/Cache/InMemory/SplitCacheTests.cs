using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Common;
using Splitio.Services.Filters;
using Splitio.Services.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class SplitCacheTests
    {
        private readonly Mock<IFlagSetsFilter> _flagSetsFilter;
        private bool SdkUpdateFlag = false;
        private EventMetadata eMetadata = null;
        public event EventHandler<EventMetadata> SdkUpdate;
        public event EventHandler<EventMetadata> SdkReady;

        public SplitCacheTests()
        {
            _flagSetsFilter = new Mock<IFlagSetsFilter>();
        }

        [TestMethod]
        public void AddAndGetSplitTest()
        {
            //Arrange
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), _flagSetsFilter.Object, internalEventsTask.Object);
            var splitName = "test1";

            //Act
            splitCache.Update(new List<ParsedSplit> { new ParsedSplit() { name = splitName } }, new List<string>(), -1);
            var result = splitCache.GetSplit(splitName);

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void AddDuplicateSplitTest()
        {
            //Arrange
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), _flagSetsFilter.Object, internalEventsTask.Object);
            var splitName = "test1";

            //Act
            var parsedSplit1 = new ParsedSplit() { name = splitName, defaultTreatment = "on" };
            var parsedSplit2 = new ParsedSplit() { name = splitName, defaultTreatment = "off" };
            splitCache.Update(new List<ParsedSplit> { parsedSplit1, parsedSplit2 }, new List<string>(), -1);
            
            var result = splitCache.GetAllSplits();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("off", result[0].defaultTreatment);
        }

        [TestMethod]
        public void GetInexistentSplitTest()
        {
            //Arrange
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), _flagSetsFilter.Object, internalEventsTask.Object);
            var splitName = "test1";

            //Act
            var result = splitCache.GetSplit(splitName);

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void RemoveSplitTest()
        {
            //Arrange
            var splitName = "test1";
            var splits = new ConcurrentDictionary<string, ParsedSplit>();
            splits.TryAdd(splitName, new ParsedSplit() { name = splitName });
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var splitCache = new InMemorySplitCache(splits, _flagSetsFilter.Object, internalEventsTask.Object);
            
            //Act
            splitCache.Update(new List<ParsedSplit>(), new List<string> { splitName }, -1);
            var result = splitCache.GetSplit(splitName);

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SetAndGetChangeNumberTest()
        {
            //Arrange
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), _flagSetsFilter.Object, internalEventsTask.Object);
            var changeNumber = 1234;

            //Act
            splitCache.SetChangeNumber(changeNumber);
            var result = splitCache.GetChangeNumber();

            //Assert
            Assert.AreEqual(changeNumber, result);
        }

        [TestMethod]
        public void GetAllSplitsTest()
        {
            //Arrange
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), _flagSetsFilter.Object, internalEventsTask.Object);
            var splitName = "test1";
            var splitName2 = "test2";

            //Act
            var split1 = new ParsedSplit() { name = splitName };
            var split2 = new ParsedSplit() { name = splitName2 };
            splitCache.Update(new List<ParsedSplit> { split1, split2 }, new List<string>(), -1);

            var result = splitCache.GetAllSplits();

            //Assert
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void AddOrUpdate_WhenUpdateTraffictType_ReturnsTrue()
        {
            // Arrange 
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), _flagSetsFilter.Object, internalEventsTask.Object);

            var splitName = "split_1";
            var splitName2 = "split_2";

            var split = new ParsedSplit { name = splitName, trafficTypeName = "traffic_type_1" };
            var split2 = new ParsedSplit { name = splitName, trafficTypeName = "traffic_type_2" };
            var split3 = new ParsedSplit { name = splitName, trafficTypeName = "traffic_type_3" };
            var split4 = new ParsedSplit { name = splitName2, trafficTypeName = "traffic_type_4" };

            splitCache.Update(new List<ParsedSplit> { split, split2, split3, split4 }, new List<string>(), -1);

            // Act
            var result1 = splitCache.TrafficTypeExists("traffic_type_1");
            var result2 = splitCache.TrafficTypeExists("traffic_type_2");
            var result3 = splitCache.TrafficTypeExists("traffic_type_3");

            // Assert
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
            Assert.IsTrue(result3);
        }

        [TestMethod]
        public void GetNamesByFlagSetsWithoutFilter()
        {
            // Arrange.
            var featureFlags = new ConcurrentDictionary<string, ParsedSplit>();
            featureFlags.TryAdd("flag-1", new ParsedSplit
            {
                name = "flag-1",
                defaultTreatment = "off",
                Sets = new HashSet<string> { "set1", "set2"}
            });

            featureFlags.TryAdd("flag-2", new ParsedSplit
            {
                name = "flag-2",
                defaultTreatment = "on",
                Sets = new HashSet<string> { "set1", "set2" }
            });

            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var splitCache = new InMemorySplitCache(featureFlags, new FlagSetsFilter(new HashSet<string>()), internalEventsTask.Object);
            var flagSetNames = new List<string> { "set1", "set2", "set3", "set4" };

            // Act.
            var result = splitCache.GetNamesByFlagSets(flagSetNames);

            // Assert.
            Assert.AreEqual(4, result.Count);
            var set1 = result["set1"];
            Assert.AreEqual(2, set1.Count);
            var set2 = result["set2"];
            Assert.AreEqual(2, set2.Count);
            var set3 = result["set3"];
            Assert.IsFalse(set3.Any());
            var set4 = result["set4"];
            Assert.IsFalse(set4.Any());
        }

        [TestMethod]
        public void GetNamesByFlagSetsWithFilters()
        {
            // Arrange.
            var featureFlags = new ConcurrentDictionary<string, ParsedSplit>();
            featureFlags.TryAdd("flag-1", new ParsedSplit
            {
                name = "flag-1",
                defaultTreatment = "off",
                Sets = new HashSet<string> { "set1", "set3" }
            });

            featureFlags.TryAdd("flag-2", new ParsedSplit
            {
                name = "flag-2",
                defaultTreatment = "on",
                Sets = new HashSet<string> { "set5", "set4" }
            });

            featureFlags.TryAdd("flag-3", new ParsedSplit
            {
                name = "flag-3",
                defaultTreatment = "on",
                Sets = new HashSet<string> { "set1", "set2" }
            });
            Mock<IInternalEventsTask> internalEventsTask = new Mock<IInternalEventsTask>();
            var splitCache = new InMemorySplitCache(featureFlags, new FlagSetsFilter(new HashSet<string>() { "set1", "set2" }), internalEventsTask.Object);
            var flagSetNames = new List<string> { "set1", "set2", "set3", "set4" };

            // Act.
            var result = splitCache.GetNamesByFlagSets(flagSetNames);

            // Assert.
            Assert.AreEqual(4, result.Count);
            var set1 = result["set1"];
            Assert.AreEqual(2, set1.Count);
            var set2 = result["set2"];
            Assert.AreEqual(1, set2.Count);
            var set3 = result["set3"];
            Assert.IsFalse(set3.Any());
            var set4 = result["set4"];
            Assert.IsFalse(set4.Any());
        }

        [TestMethod]
        public void NotifyUpdateEventTest()
        {
            // Arrange.
            var eventsManager = new EventsManager<SdkEvent, SdkInternalEvent, EventMetadata>(new EventsManagerConfig(), new EventDelivery<SdkEvent, EventMetadata>());
            var internalEventsTask = new InternalEventsTask(eventsManager, new Splitio.Services.Shared.Classes.SplitQueue<Splitio.Services.EventSource.Workers.SdkEventNotification>());
            internalEventsTask.Start();
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), _flagSetsFilter.Object, internalEventsTask);
            var splitName = "test1";

            var toNotify = new List<string> { { splitName } };
            SdkUpdate += sdkUpdate_callback;
            eventsManager.Register(SdkEvent.SdkUpdate, TriggerSdkUpdate);
            eventsManager.Register(SdkEvent.SdkReady, TriggerSdkReady);
            eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady, null);

            // Act.
            SdkUpdateFlag = false;
            splitCache.Update(new List<ParsedSplit> { new ParsedSplit() { name = splitName } }, new List<string>(), -1);
            SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));

            // Assert.
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.FlagsUpdate, eMetadata.GetEventType());
            Assert.IsTrue(eMetadata.GetNames().Count == 1);
            Assert.IsTrue(eMetadata.GetNames().Contains(splitName));

            // Act.
            SdkUpdateFlag = false;
            eMetadata = null;
            splitCache.Kill(123, splitName, "off");
            SpinWait.SpinUntil(() => SdkUpdateFlag, TimeSpan.FromMilliseconds(1000));

            // Assert.
            Assert.IsTrue(SdkUpdateFlag);
            Assert.AreEqual(SdkEventType.FlagsUpdate, eMetadata.GetEventType());
            Assert.IsTrue(eMetadata.GetNames().Count == 1);
            Assert.IsTrue(eMetadata.GetNames().Contains(splitName));

            // Act.
            SdkUpdateFlag = false;
            splitCache.Update(new List<ParsedSplit>(), new List<string>(), 1234);

            // Assert.
            Assert.IsFalse(SdkUpdateFlag);
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
