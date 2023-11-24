using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Cache.Filter;

namespace Splitio_Tests.Unit_Tests.Cache.Filter
{
    [TestClass]
    public class FilterAdapterTests
    {
        [TestMethod]
        public void Test()
        {
            // Arrange.
            var bf = new BloomFilter(expectedElements: 1000, errorRate: 0.01);
            var adapter = new FilterAdapter(filter: bf);

            // Act & Assert.
            Assert.IsTrue(adapter.Add("feature-name-01", "key-test-01"));
            Assert.IsTrue(adapter.Add("feature-name-01", "key-test-02"));
            Assert.IsTrue(adapter.Add("feature-name-01", "key-test-03"));
            Assert.IsTrue(adapter.Add("feature-name-02", "key-test-01"));
            Assert.IsFalse(adapter.Add("feature-name-01", "key-test-01"));
            
            Assert.IsTrue(adapter.Contains("feature-name-01", "key-test-01"));
            Assert.IsTrue(adapter.Contains("feature-name-01", "key-test-02"));
            Assert.IsTrue(adapter.Contains("feature-name-01", "key-test-03"));
            Assert.IsTrue(adapter.Contains("feature-name-02", "key-test-01"));
            Assert.IsFalse(adapter.Contains("feature-name-10", "key-test-01"));

            adapter.Clear();
        }
    }
}
