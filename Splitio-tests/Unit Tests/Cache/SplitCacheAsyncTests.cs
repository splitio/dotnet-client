using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Cache.Classes;
using Splitio.Services.Cache.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Cache
{
    [TestClass]
    public class SplitCacheAsyncTests
    {
        private readonly IFeatureFlagCache _cache;

        public SplitCacheAsyncTests()
        {
            var splits = new ConcurrentDictionary<string, ParsedSplit>();

            _cache = new InMemorySplitCache(splits);
        }

        [TestMethod]
        public async Task ExecuteAddSplitAsyncSuccessful()
        {
            // Arrange
            var splitName = "split-name-1";
            var parsedSplit = new ParsedSplit() { name = splitName };

            // Act
            await _cache.AddSplitAsync(splitName, parsedSplit);
            var split = await _cache.GetSplitAsync(splitName);

            // Assert
            Assert.AreEqual(splitName, split.name);
        }

        [TestMethod]
        public async Task ExecuteAddOrUpdateAsyncSuccessful()
        {
            // Arrange
            var splitName = "split-name-2";
            var parsedSplit = new ParsedSplit() { name = splitName };

            // Act & Assert
            var result = await _cache.AddOrUpdateAsync(splitName, parsedSplit);
            Assert.IsFalse(result);

            result = await _cache.AddOrUpdateAsync(splitName, parsedSplit);
            Assert.IsTrue(result);

            var split = await _cache.GetSplitAsync(splitName);
            Assert.AreEqual(splitName, split.name);
        }

        [TestMethod]
        public async Task ExecuteRemoveSplitAsyncSuccessful()
        {
            // Arrange
            var splitName = "split-name-3";
            var parsedSplit = new ParsedSplit() { name = splitName };

            await _cache.AddSplitAsync(splitName, parsedSplit);

            // Act
            var result = await _cache.RemoveSplitAsync(splitName);

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(await _cache.RemoveSplitAsync("split-test"));
        }

        [TestMethod]
        public async Task ExecuteSetChangeNumberAsyncSuccessful()
        {
            // Act
            await _cache.SetChangeNumberAsync(10);
            var result = await _cache.GetChangeNumberAsync();

            // Assert
            Assert.AreEqual(10, result);
        }

        [TestMethod]
        public async Task ExecuteClearAsyncSuccessful()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                var splitName = "split-name-" + i;
                var parsedSplit = new ParsedSplit() { name = splitName };

                await _cache.AddSplitAsync(splitName, parsedSplit);
            }

            // Act & Assert
            var splits = await _cache.GetAllSplitsAsync();
            Assert.AreEqual(5, splits.Count);

            await _cache.ClearAsync();

            splits = await _cache.GetAllSplitsAsync();
            Assert.IsFalse(splits.Any());
        }

        [TestMethod]
        public async Task ExecuteKillAsyncSuccessful()
        {
            // Arrange
            var splitName = "split-name-5";
            var parsedSplit = new ParsedSplit() { name = splitName, changeNumber = 5, defaultTreatment = "on" };
            var changeNumber = 111;
            var defaultTreatment = "off";

            await _cache.AddSplitAsync(splitName, parsedSplit);

            // Act & Assert
            var split = await _cache.GetSplitAsync(splitName);
            Assert.AreEqual(splitName, split.name);
            Assert.AreEqual(5, split.changeNumber);
            Assert.AreEqual("on", split.defaultTreatment);

            await _cache.KillAsync(changeNumber, splitName, defaultTreatment);

            split = await _cache.GetSplitAsync(splitName);
            Assert.AreEqual(splitName, split.name);
            Assert.AreEqual(changeNumber, split.changeNumber);
            Assert.AreEqual(defaultTreatment, split.defaultTreatment);
        }

        [TestMethod]
        public async Task ExecuteGetSplitNamesAsyncSuccessful()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                var splitName = "split-name-" + i;
                var parsedSplit = new ParsedSplit() { name = splitName };

                await _cache.AddSplitAsync(splitName, parsedSplit);
            }

            // Act
            var result = await _cache.GetSplitNamesAsync();

            // Assert
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("split-name-0"));
            Assert.IsTrue(result.Contains("split-name-1"));
            Assert.IsTrue(result.Contains("split-name-2"));
            Assert.IsTrue(result.Contains("split-name-3"));
            Assert.IsTrue(result.Contains("split-name-4"));
        }

        [TestMethod]
        public async Task ExecuteSplitsCountAsyncSuccessful()
        {
            // Arrange
            for (int i = 0; i < 15; i++)
            {
                var splitName = "split-name-" + i;
                var parsedSplit = new ParsedSplit() { name = splitName };

                await _cache.AddSplitAsync(splitName, parsedSplit);
            }

            // Act
            var result = await _cache.SplitsCountAsync();

            // Assert
            Assert.AreEqual(15, result);
        }

        [TestMethod]
        public async Task ExecuteTrafficTypeExistsAsyncSuccessful()
        {
            // Arrange
            var traficType = "tt-test";
            var splitName = "split-name";
            var parsedSplit = new ParsedSplit() { name = splitName, trafficTypeName = traficType };

            await _cache.AddSplitAsync(splitName, parsedSplit);

            // Act & Assert
            var exists = await _cache.TrafficTypeExistsAsync(traficType);
            Assert.IsTrue(exists);

            exists = await _cache.TrafficTypeExistsAsync("user");
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public async Task ExecuteFetchManyAsyncSuccessful()
        {
            // Arrange
            var splitNames = new List<string>();

            for (int i = 0; i < 3; i++)
            {
                var splitName = "split-name-" + i;
                splitNames.Add(splitName);

                var parsedSplit = new ParsedSplit() { name = splitName };

                await _cache.AddSplitAsync(splitName, parsedSplit);
            }

            // Act & Assert
            var result = await _cache.FetchManyAsync(splitNames);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Any(s => s.name == "split-name-0"));
            Assert.IsTrue(result.Any(s => s.name == "split-name-1"));
            Assert.IsTrue(result.Any(s => s.name == "split-name-2"));

            result = await _cache.FetchManyAsync(new List<string>() { "split-name-10", "split-name-11" });
            Assert.IsFalse(result.Any());
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await _cache.ClearAsync();
        }
    }
}
