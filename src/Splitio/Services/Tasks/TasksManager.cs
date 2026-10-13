using Splitio.Commons.Cache.Interfaces;
using Splitio.Commons.Dto;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    public class TasksManager : ITasksManager
    {
        private readonly ConcurrentDictionary<Commons.Dto.SplitTaskName, ISplitTask> _tasks = new ConcurrentDictionary<Commons.Dto.SplitTaskName, ISplitTask>();

        private readonly IStatusManager _statusManager;

        public TasksManager(IStatusManager statusManager)
        {
            _statusManager = statusManager;
        }

        public ISplitTask NewOnTimeTaskAndStart(Commons.Dto.SplitTaskName taskName, Func<Task> function)
        {
            var task = new SplitOneTimeTask(_statusManager, taskName);
            task.SetFunction(function);

            task.Start();

            return task;
        }

        public ISplitTask NewOnTimeTaskAndStart(Commons.Dto.SplitTaskName taskName, Action action)
        {
            var task = new SplitOneTimeTask(_statusManager, taskName);
            task.SetAction(action);

            task.Start();

            return task;
        }

        public ISplitTask NewOnTimeTask(Commons.Dto.SplitTaskName taskName)
        {
            var task = new SplitOneTimeTask(_statusManager, taskName);
            AddOrUpdate(taskName, task);

            return task;
        }

        public ISplitTask NewScheduledTask(Commons.Dto.SplitTaskName taskName, int intervalMs)
        {
            var task = new SplitOneTimeTask(_statusManager, taskName, intervalMs);
            AddOrUpdate(taskName, task);

            return task;
        }

        public ISplitTask NewPeriodicTask(Commons.Dto.SplitTaskName taskName, int intervalMs)
        {
            var task = new SplitPeriodicTask(_statusManager, taskName, intervalMs);
            AddOrUpdate(taskName, task);

            return task;
        }

        public async Task DestroyAsync()
        {
            foreach (var task in _tasks.Values)
            {
                if (task.IsRunning())
                    await task.StopAsync();
            }
        }

        private void AddOrUpdate(Commons.Dto.SplitTaskName name, ISplitTask task)
        {
            _tasks.TryRemove(name, out _);
            _tasks.TryAdd(name, task);
        }
    }
}
