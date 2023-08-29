using Splitio.Services.Cache.Interfaces;
using System;
using System.Collections.Generic;

namespace Splitio.Services.Tasks
{
    public class TasksManager : ITasksManager
    {
        private readonly List<ISplitTask> _tasks = new List<ISplitTask>();

        public ISplitTask NewOnTimeTaskAndStart(IStatusManager statusManager, Enums.Task taskName, Action action)
        {
            var task = new SplitOneTimeTask(statusManager, taskName);
            task.SetAction(action);

            _tasks.Add(task);

            task.Start();

            return task;
        }

        public ISplitTask NewOnTimeTask(IStatusManager statusManager, Enums.Task taskName)
        {
            var task = new SplitOneTimeTask(statusManager, taskName);
            _tasks.Add(task);

            return task;
        }

        public ISplitTask NewScheduledTask(IStatusManager statusManager, Enums.Task taskName, int intervalMs)
        {
            var task = new SplitOneTimeTask(statusManager, taskName, intervalMs);
            _tasks.Add(task);

            return task;
        }

        public ISplitTask NewPeriodicTask(IStatusManager statusManager, Enums.Task taskName, int intervalMs)
        {
            var task = new SplitPeriodicTask(statusManager, taskName, intervalMs);
            _tasks.Add(task);

            return task;
        }

        public void Destroy()
        {
            foreach (var task in _tasks)
            {
                try
                {
                    if (task.IsRunning())
                        task.Stop();
                }
                catch { }
            }
        }
    }
}
