using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Shared
{
    [TestClass]
    public class SplitQueueTests
    {
        [TestMethod]
        public async Task EnqueueItemsAndCheckCount()
        {
            // Arrange.
            var queue = new SplitQueue<string>();

            // Act.
            await queue.EnqueueAsync("test-1");
            await queue.EnqueueAsync("test-2");

            // Assert.
            Assert.AreEqual(2, queue.Count());
        }

        [TestMethod]
        public async Task EnqueueItemsWithListener()
        {
            // Arrange.
            var queue = new SplitQueue<string>();
            var listener = new QueueListenerForTest(queue);
            queue.AddObserver(listener);

            // Act.
            await queue.EnqueueAsync("test-1");
            await queue.EnqueueAsync("test-2");
            await queue.EnqueueAsync("test-3");
            Thread.Sleep(1000);

            // Assert.
            Assert.AreEqual(3, listener.Count());
        }

        [TestMethod]
        public async Task EnqueueItemsWithoutListener()
        {
            // Arrange.
            var queue = new SplitQueue<string>();
            var listener = new QueueListenerForTest(queue);

            // Act.
            await queue.EnqueueAsync("test-1");
            await queue.EnqueueAsync("test-2");
            await queue.EnqueueAsync("test-3");

            // Assert.
            Assert.AreEqual(0, listener.Count());
        }

        [TestMethod]
        public async Task EnqueueMultipleItemsWithoutListener()
        {
            // Arrange.
            var queue = new SplitQueue<string>();
            var listener = new QueueListenerForTest(queue);

            // Act.
            await queue.EnqueueAsync(new List<string> { "test-1", "test-3", "test-2" });

            // Assert.
            Assert.AreEqual(0, listener.Count());
        }

        [TestMethod]
        public async Task EnqueueMultipleItemsWithListener()
        {
            // Arrange.
            var queue = new SplitQueue<string>();
            var listener = new QueueListenerForTest(queue);
            queue.AddObserver(listener);

            // Act.
            await queue.EnqueueAsync(new List<string> { "test-1", "test-3", "test-2" });

            // Assert.
            Assert.AreEqual(0, listener.Count());
        }
    }

    public class QueueListenerForTest : IQueueObserver
    {
        private readonly List<string> _items = new List<string>();
        private readonly SplitQueue<string> _queue;

        public QueueListenerForTest(SplitQueue<string> queue)
        {
            _queue = queue;
        }

        public Task Notify()
        {
            if (_queue.TryDequeue(out string item))
            {
                _items.Add(item);
            }

            return Task.FromResult(0);
        }

        public int Count()
        {
            return _items.Count;
        }
    }
}
