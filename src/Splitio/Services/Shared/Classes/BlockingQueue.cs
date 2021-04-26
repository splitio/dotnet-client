using System.Collections.Concurrent;

namespace Splitio.Services.Shared.Classes
{
    public class BlockingQueue<T>
    {
        private ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly object _lockingObject = new object();

        private readonly int _maxSize;

        public BlockingQueue(int maxSize)
        {
            _maxSize = maxSize;
        }

        public bool HasReachedMaxSize()
        {
            return _queue.Count >= _maxSize;
        }

        public ConcurrentQueue<T> FetchAll()
        {
            lock (_lockingObject)
            {
                var existingItems = new ConcurrentQueue<T>(_queue);
                return existingItems;
            }
        }

        public ConcurrentQueue<T> FetchAllAndClear()
        {
            lock (_lockingObject)
            {
                var existingItems = new ConcurrentQueue<T>(_queue);
                _queue = new ConcurrentQueue<T>();
                return existingItems;
            }
        }

        public bool Enqueue(T item)
        {
            lock (_lockingObject)
            {
                if (HasReachedMaxSize()) return false;

                _queue.Enqueue(item);
                return true;
            }
        }
        public T Dequeue()
        {
            lock (_lockingObject)
            {
                _queue.TryDequeue(out T item);
                return item;
            }
        }
    }
}
