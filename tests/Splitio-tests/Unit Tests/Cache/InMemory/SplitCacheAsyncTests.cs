using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Filters;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class SplitCacheAsyncTests
    {
        private readonly IFlagSetsFilter _flagSetsFilter;
        private readonly IFeatureFlagCache _cache;

        public SplitCacheAsyncTests()
        {
            _flagSetsFilter = new FlagSetsFilter(new HashSet<string>());
            var splits = new ConcurrentDictionary<string, ParsedSplit>();

            _cache = new InMemorySplitCache(splits, _flagSetsFilter);
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
                Name = ffName,
                DefaultTreatment = "on",
                ChangeNumber = 1
            };

            _cache.Update(new List<ParsedSplit> { split }, new List<string>(), -1);

            // Act.
            var result = await _cache.GetSplitAsync(ffName);

            // Assert.
            Assert.AreEqual(ffName, result.Name);
            Assert.AreEqual("on", result.DefaultTreatment);
            Assert.AreEqual(1, result.ChangeNumber);
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
                Name = "feature-flag-1",
                DefaultTreatment = "on",
                ChangeNumber = 1
            };

            var split2 = new ParsedSplit
            {
                Name = "feature-flag-2",
                DefaultTreatment = "on",
                ChangeNumber = 1
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
                    Name = $"feature-flag-{i}",
                    DefaultTreatment = "on",
                    ChangeNumber = i
                });
            }

            _cache.Update(toAdd, new List<string>(), -1);

            // Act.
            var result = await _cache.FetchManyAsync(new List<string> { "feature-flag-2" });

            // Assert.
            Assert.AreEqual(1, result.Count);
            var ff = result.FirstOrDefault();
            Assert.AreEqual("feature-flag-2", ff.Name);
            Assert.AreEqual("on", ff.DefaultTreatment);
            Assert.AreEqual(2, ff.ChangeNumber);
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
                    Name = $"feature-flag-{i}",
                    DefaultTreatment = "on",
                    ChangeNumber = i
                });
            }

            _cache.Update(toAdd, new List<string>(), -1);

            // Act.
            var result = await _cache.GetSplitNamesAsync();

            // Assert.
            Assert.AreEqual(5, result.Count);
        }
    }
}
