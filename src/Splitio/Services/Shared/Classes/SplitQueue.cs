using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Classes
{
    public interface IQueueObserver
    {
        Task Notify();
    }

    public class SplitQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue;
        private IQueueObserver _observer;
        private Task _task;

        public SplitQueue()
        {
            _queue = new ConcurrentQueue<T>();
        }

        public async Task EnqueueAsync(T item)
        {
            _queue.Enqueue(item);
            await NotifyAsync();
        }

        public bool TryDequeue(out T item)
        {
            return _queue.TryDequeue(out item);
        }

        public void AddObserver(IQueueObserver observer)
        {
            _observer = observer;
        }

        public int Count()
        {
            return _queue.Count;
        }

        private async Task NotifyAsync()
        {
            if (_observer == null) return;

            if (_task != null)
            {
                await _task;
            }

            _task = Task.Factory.StartNew(() => _observer.Notify());
        }
    }
}
