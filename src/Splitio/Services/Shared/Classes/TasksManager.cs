using Splitio.CommonLibraries;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Classes
{
    public class TasksManager : ITasksManager
    {
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(IWrapperAdapter));

        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly List<Task> _tasks;

        public TasksManager(IWrapperAdapter wrapperAdapter)
        {
            _tasks = new List<Task>();
            _wrapperAdapter = wrapperAdapter;
        }

        public void Start(Action action, CancellationTokenSource cancellationToken, string description)
        {
            _log.Debug($"Starting Task: {description}");
            Task.Factory.StartNew(action, cancellationToken.Token);
            //_tasks.Add(task);
        }

        public void StartPeriodic(Action action, int intervalInMilliseconds, CancellationTokenSource cancellationToken, string description)
        {
            _log.Debug($"Starting Periodic Task: {description}");
            PeriodicTaskFactory.Start(action, intervalInMilliseconds, cancellationToken.Token);
            //_tasks.Add(task);
        }
    }
}
