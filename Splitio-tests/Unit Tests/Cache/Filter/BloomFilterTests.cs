using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Cache.Filter;
using System;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests.Cache.Filter
{
    [TestClass]
    public class BloomFilterTests
    {
        [TestMethod]
        public void Test()
        {
            // Arrange.
            var expectedKeys = new List<string>
            {
                $"key-{Guid.NewGuid()}",
                $"key-{Guid.NewGuid()}",
                $"key-{Guid.NewGuid()}",
                $"key-{Guid.NewGuid()}",
                $"key-{Guid.NewGuid()}",
                $"key-{Guid.NewGuid()}",
                $"key-{Guid.NewGuid()}",
            };
            var bf = new BloomFilter(expectedElements: 1000, errorRate: 0.01);

            // Act.
            for (int i = 0; i < 500; i++)
            {
                Assert.IsTrue(bf.Add($"key-{Guid.NewGuid()}"));
            }

            foreach (var item in expectedKeys)
            {
                Assert.IsTrue(bf.Add(item));
            }

            // Assert.
            foreach (var item in expectedKeys)
            {
                Assert.IsFalse(bf.Add(item));
            }

            foreach (var item in expectedKeys)
            {
                Assert.IsTrue(bf.Contains(item));
            }

            foreach (var item in expectedKeys)
            {
                Assert.IsFalse(bf.Contains($"{item}-fail"));
            }

            bf.Clear();
        }
    }
}
