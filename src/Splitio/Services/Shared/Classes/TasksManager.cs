using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;

namespace Splitio.Services.Shared.Classes
{
    public class TasksManager : ITasksManager
    {
        private readonly List<ISplitTask> _tasks = new List<ISplitTask>();

        private readonly IStatusManager _statusManager;

        public TasksManager(IStatusManager statusManager)
        {
            _statusManager = statusManager;
        }

        public ISplitTask NewOnTimeTask(Enums.Task taskName)
        {
            var task = new SplitTask(taskName, 0.01, _statusManager, false);
            _tasks.Add(task);

            return task;
        }

        public ISplitTask NewPeriodicTask(Enums.Task taskName, int intervalMs)
        {
            var task = new SplitTask(taskName, intervalMs, _statusManager, true);
            _tasks.Add(task);

            return task;
        }

        public void Destroy()
        {
            foreach (var task in _tasks)
            {
                if (task.IsRunning()) task.Kill();
            }
        }
    }
}
