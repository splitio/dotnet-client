using Splitio.CommonLibraries;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Classes
{
    public class TasksManager : ITasksManager
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(IWrapperAdapter));

        private readonly IWrapperAdapter _wrapperAdapter;
        public TasksManager(IWrapperAdapter wrapperAdapter)
        {
            _wrapperAdapter = wrapperAdapter;
        }
        public void Start(Action action, CancellationTokenSource cancellationToken, string description)
        {
            _log.Debug($"Starting Task: {description}");
            Task.Factory.StartNew(action, cancellationToken.Token);
        }

        public void Start(Action action, string description)
        {
            Start(action, new CancellationTokenSource(), description);
        }

        public void Start(Func<Task> function, CancellationTokenSource cancellationToken, string description)
        {
            _log.Debug($"Starting Task: {description}");
            Task.Factory.StartNew(function, cancellationToken.Token);
        }

        public void StartPeriodic(Action action, int intervalInMilliseconds, CancellationTokenSource cancellationToken, string description)
        {
            _log.Debug($"Starting Periodic Task: {description}");
            PeriodicTaskFactory.Start(action, intervalInMilliseconds, cancellationToken.Token);
        }
    }
}
