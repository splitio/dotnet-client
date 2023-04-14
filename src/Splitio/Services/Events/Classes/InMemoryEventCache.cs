using Splitio.Domain;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Events.Classes
{
    public class InMemoryEventCache : IEventCache
    {
        private readonly BlockingQueue<WrappedEvent> _queue;

        public InMemoryEventCache(BlockingQueue<WrappedEvent> queue)
        {
            _queue = queue;
        }

        public int Add(WrappedEvent wrappedEvent)
        {
            if (_queue == null) return 0;

            var droppedItems = 0;

            if (!_queue.Enqueue(wrappedEvent)) droppedItems++;

            return droppedItems;
        }

        public List<WrappedEvent> FetchAllAndClear()
        {
            return _queue?.FetchAllAndClear().ToList();
        }

        public bool HasReachedMaxSize()
        {
            return _queue != null && _queue.HasReachedMaxSize();
        }

        public bool IsEmpty()
        {
            return _queue?.IsEmpty ?? false;
        }
    }
}
