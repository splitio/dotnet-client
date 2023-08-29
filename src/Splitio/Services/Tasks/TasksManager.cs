using Splitio.Services.Cache.Interfaces;
using System;
using System.Collections.Concurrent;

namespace Splitio.Services.Tasks
{
    public class TasksManager : ITasksManager
    {
        private readonly ConcurrentDictionary<Enums.Task, ISplitTask> _tasks = new ConcurrentDictionary<Enums.Task, ISplitTask>();

        private readonly IStatusManager _statusManager;

        public TasksManager(IStatusManager statusManager)
        {
            _statusManager = statusManager;
        }

        public ISplitTask NewOnTimeTaskAndStart(Enums.Task taskName, Action action)
        {
            var task = new SplitOneTimeTask(_statusManager, taskName);
            task.SetAction(action);

            task.Start();

            return task;
        }

        public ISplitTask NewOnTimeTask(Enums.Task taskName)
        {
            var task = new SplitOneTimeTask(_statusManager, taskName);
            AddOrUpdate(taskName, task);

            return task;
        }

        public ISplitTask NewScheduledTask(Enums.Task taskName, int intervalMs)
        {
            var task = new SplitOneTimeTask(_statusManager, taskName, intervalMs);
            AddOrUpdate(taskName, task);

            return task;
        }

        public ISplitTask NewPeriodicTask(Enums.Task taskName, int intervalMs)
        {
            var task = new SplitPeriodicTask(_statusManager, taskName, intervalMs);
            AddOrUpdate(taskName, task);

            return task;
        }

        public void Destroy()
        {
            foreach (var task in _tasks.Values)
            {
                if (task.IsRunning())
                    task.Stop();
            }
        }

        private void AddOrUpdate(Enums.Task name, ISplitTask task)
        {
            if (_tasks.TryGetValue(name, out ISplitTask oldTask))
            {
                oldTask.Stop();
                _tasks.TryRemove(name, out _);
            }

            _tasks.TryAdd(name, task);
        }
    }
}
