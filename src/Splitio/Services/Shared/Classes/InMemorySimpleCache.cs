﻿using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Classes
{
    public class InMemorySimpleCache<T> : ISimpleProducerCache<T>
    {
        private readonly BlockingQueue<T> _queue;

        public InMemorySimpleCache(BlockingQueue<T> queue)
        {
            _queue = queue;
        }

        public Task<int> AddItemsAsync(IList<T> items)
        {
            return Task.FromResult(AddItems(items));
        }

        public int AddItems(IList<T> items)
        {
            if (_queue == null) return 0;

            var droppedItems = 0;

            foreach (var item in items)
            {
                var added = _queue.Enqueue(item);

                if (!added) droppedItems++;
            }

            return droppedItems;
        }

        public List<T> FetchAllAndClear()
        {
            return _queue != null ? _queue.FetchAllAndClear().ToList() : null;
        }

        public bool HasReachedMaxSize()
        {
            return _queue != null ? _queue.HasReachedMaxSize() : false;
        }

        public bool IsEmpty()
        {
            return _queue?.IsEmpty ?? false;
        }
    }
}
