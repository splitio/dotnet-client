using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Filters;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class SplitCacheTests
    {

        [TestMethod]
        public void AddAndGetSplitTest()
        {
            //Arrange
            var filter = new FlagSetsFilter(new HashSet<string>());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), filter);
            var splitName = "test1";

            var toAdd = new List<ParsedSplit> { new ParsedSplit() { name = splitName } };

            //Act
            splitCache.Update(toAdd, new List<ParsedSplit>(), 1);
            var result = splitCache.GetSplit(splitName);

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void AddDuplicateSplitTest()
        {
            //Arrange
            var filter = new FlagSetsFilter(new HashSet<string>());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), filter);
            var splitName = "test1";
            var parsedSplit1 = new ParsedSplit() { name = splitName };
            var parsedSplit2 = new ParsedSplit() { name = splitName };

            var toAdd = new List<ParsedSplit> { parsedSplit1, parsedSplit2 };

            //Act
            splitCache.Update(toAdd, new List<ParsedSplit>(), 1);
            var result = splitCache.GetAllSplits();

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(result[0].name, parsedSplit1.name);
        }

        [TestMethod]
        public void GetInexistentSplitTest()
        {
            //Arrange
            var filter = new FlagSetsFilter(new HashSet<string>());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), filter);
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

            var filter = new FlagSetsFilter(new HashSet<string>());
            var splitCache = new InMemorySplitCache(splits, filter);

            var toRemove = new List<ParsedSplit> { new ParsedSplit { name = splitName } };
            
            //Act
            splitCache.Update(new List<ParsedSplit>(), toRemove, 1);
            var result = splitCache.GetSplit(splitName);

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SetAndGetChangeNumberTest()
        {
            //Arrange
            var filter = new FlagSetsFilter(new HashSet<string>());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), filter);
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
            var filter = new FlagSetsFilter(new HashSet<string>());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), filter);
            var splitName = "test1";
            var splitName2 = "test2";

            var toAdd = new List<ParsedSplit>
            {
                new ParsedSplit() { name = splitName },
                new ParsedSplit() { name = splitName2 }
            };

            //Act
            splitCache.Update(toAdd, new List<ParsedSplit>(), 1);

            var result = splitCache.GetAllSplits();

            //Assert
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void AddOrUpdate_WhenUpdateTraffictType_ReturnsTrue()
        {
            // Arrange
            var filter = new FlagSetsFilter(new HashSet<string>());
            var splitCache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>(), filter);

            var splitName = "split_1";
            var splitName2 = "split_2";

            var toAdd = new List<ParsedSplit>
            {
                new ParsedSplit { name = splitName, trafficTypeName = "traffic_type_1" },
                new ParsedSplit { name = splitName, trafficTypeName = "traffic_type_2" },
                new ParsedSplit { name = splitName, trafficTypeName = "traffic_type_3" },
                new ParsedSplit { name = splitName2, trafficTypeName = "traffic_type_4" }
            };

            splitCache.Update(toAdd, new List<ParsedSplit>(), 1);

            // Act
            var result1 = splitCache.TrafficTypeExists("traffic_type_1");
            var result2 = splitCache.TrafficTypeExists("traffic_type_2");
            var result3 = splitCache.TrafficTypeExists("traffic_type_3");

            // Assert
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
            Assert.IsTrue(result3);
        }

        // TODO: check this
        //[TestMethod]
        //public void Test()
        //{
        //    // Arrange
        //    var cache = new InMemorySplitCache(new ConcurrentDictionary<string, ParsedSplit>());

        //    // Act
        //    var ff = new ParsedSplit
        //    {
        //        name = "mauro",
        //        Sets = new HashSet<string>
        //        {
        //            "set_a", "set_b", "set_c"
        //        }
        //    };
        //    cache.AddOrUpdateFlagSets(ff);

        //    ff = new ParsedSplit
        //    {
        //        name = "mauro",
        //        Sets = new  HashSet<string>
        //        {
        //            "set_a", "set_b",
        //        }
        //    };
        //    cache.AddOrUpdateFlagSets(ff);

        //    // Assert

        //    var names = cache.GetNamesByFlagSets(new List<string> { "set_a", "set_b", "set_c" });

        //    names = cache.GetNamesByFlagSets(new List<string> { "set_a" });

        //    names = cache.GetNamesByFlagSets(new List<string> { "set_b" });

        //    names = cache.GetNamesByFlagSets(new List<string> { "set_v"});
        //}
    }
}
