using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Impressions.Classes
{
    public  class InMemoryImpressionCache : IImpressionCache
    {
        private readonly BlockingQueue<KeyImpression> _queue;

        public InMemoryImpressionCache(BlockingQueue<KeyImpression> queue)
        {
            _queue = queue;
        }

        public int AddItems(IList<KeyImpression> items)
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

        public List<KeyImpression> FetchAllAndClear()
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
