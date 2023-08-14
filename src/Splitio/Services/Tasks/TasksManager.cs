using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    public class TasksManager : ITasksManager
    {
        private readonly List<ISplitTask> _tasks = new List<ISplitTask>();

        public ISplitTask NewOnTimeTaskAndStart(Enums.Task taskName, Action action)
        {
            var task = new SplitOneTimeTask(taskName);
            task.SetAction(action);

            _tasks.Add(task);

            task.Start();

            return task;
        }

        public ISplitTask NewOnTimeTask(Enums.Task taskName)
        {
            var task = new SplitOneTimeTask(taskName);
            _tasks.Add(task);

            return task;
        }

        public ISplitTask NewScheduledTask(Enums.Task taskName, int intervalMs)
        {
            var task = new SplitOneTimeTask(taskName, intervalMs);
            _tasks.Add(task);

            return task;
        }

        public ISplitTask NewPeriodicTask(Enums.Task taskName, int intervalMs)
        {
            var task = new SplitPeriodicTask(taskName, intervalMs);
            _tasks.Add(task);

            return task;
        }

        public async Task DestroyAsync()
        {
            foreach (var task in _tasks)
            {
                try { await task.StopAsync(); }
                catch (Exception ex) { }
            }
        }
    }
}
