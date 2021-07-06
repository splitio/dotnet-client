using Splitio.CommonLibraries;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Classes
{
    public class TasksManager : ITasksManager
    {
        private readonly ConcurrentDictionary<Task, CancellationTokenSource> _tasks = new ConcurrentDictionary< Task, CancellationTokenSource>();
        private readonly object _lock = new object();
        private readonly int _maxCapacity = 30;

        private readonly IWrapperAdapter _wrapperAdapter;

        public TasksManager(IWrapperAdapter wrapperAdapter)
        {
            _wrapperAdapter = wrapperAdapter;
        }

        public void Start(Action action, CancellationTokenSource cancellationToken)
        {
            lock (_lock)
            {
                if (_tasks.Count >= _maxCapacity)
                {
                    ClearTasks();
                }

                var task = Task.Factory.StartNew(action, cancellationToken.Token);
                _tasks.TryAdd(task, cancellationToken);
            }
        }

        public void StartPeriodic(Action action, int intervalInMilliseconds, CancellationTokenSource cancellationToken)
        {
            lock (_lock)
            {
                if (_tasks.Count >= _maxCapacity)
                {
                    ClearTasks();
                }

                var task = PeriodicTaskFactory.Start(action, intervalInMilliseconds, cancellationToken.Token);
                _tasks.TryAdd(task, cancellationToken);
            }
        }

        public void CancelAll()
        {
            lock (_lock)
            {
                foreach (var item in _tasks)
                {
                    if (!item.Value.IsCancellationRequested)
                        item.Value.Cancel();
                }
            }
        }

        private void ClearTasks()
        {
            var tasks = _tasks.Keys;

            foreach (var t in tasks)
            {
                try
                {
                    if (t.Status == TaskStatus.RanToCompletion ||
                        t.Status == TaskStatus.Canceled ||
                        t.Status == TaskStatus.Faulted)
                    {
                        _tasks.TryRemove(t, out CancellationTokenSource tokenSource);
                        _wrapperAdapter.TaskWaitAndDispose();
                    }
                }
                catch (Exception)
                {
                    // do something
                }
            }
        }
    }
}
