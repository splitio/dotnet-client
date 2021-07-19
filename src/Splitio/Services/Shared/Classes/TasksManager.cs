using Splitio.CommonLibraries;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Classes
{
    public class TasksManager : ITasksManager
    {
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(IWrapperAdapter));

        private readonly ConcurrentDictionary<Task, CancellationTokenSource> _tasks = new ConcurrentDictionary< Task, CancellationTokenSource>();
        private readonly object _lock = new object();
        private readonly int _maxCapacity = 30;

        private readonly IWrapperAdapter _wrapperAdapter;

        public TasksManager(IWrapperAdapter wrapperAdapter)
        {
            _wrapperAdapter = wrapperAdapter;
        }

        public void Start(Action action, CancellationTokenSource cancellationToken, string description)
        {
            lock (_lock)
            {
                _log.Debug($"Starting Task: {description}");

                if (_tasks.Count >= _maxCapacity)
                {
                    ClearTasks();
                }

                var task = Task.Factory.StartNew(action, cancellationToken.Token);
                _tasks.TryAdd(task, cancellationToken);
            }
        }

        public void StartPeriodic(Action action, int intervalInMilliseconds, CancellationTokenSource cancellationToken, string description)
        {
            lock (_lock)
            {
                _log.Debug($"Starting Periodic Task: {description}");

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
            _log.Debug("Start Cleaning tasks list.");

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
                catch (Exception ex)
                {
                    _log.Debug(ex.Message);
                }
            }
        }
    }
}
