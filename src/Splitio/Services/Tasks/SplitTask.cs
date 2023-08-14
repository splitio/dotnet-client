using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    internal abstract class SplitTask : ISplitTask
    {
        protected readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitTask));

        protected readonly Enums.Task _taskName;
        protected readonly int _interval;

        protected CancellationTokenSource _cts;
        protected Action _action;
        protected Task _task;

        public SplitTask(Enums.Task taskName, int interval)
        {
            _taskName = taskName;
            _interval = interval;
        }

        public void Start()
        {
            if (IsRunning())
            {
                _log.Debug($"Task {_taskName} already running.");
            }

            _cts = new CancellationTokenSource();
            _task = DoWorkAsync();

            _log.Debug($"Task {_taskName} running.");
        }

        public async Task StopAsync()
        {
            if (!IsRunning())
            {
                _log.Debug($"Task {_taskName} is not running.");
            }

            _cts.Cancel();
            await _task;
            _cts.Dispose();
        }

        public bool IsRunning()
        {
            return !_cts?.IsCancellationRequested ?? false;
        }

        public void SetAction(Action action)
        {
            _action = action;
        }

        protected abstract Task DoWorkAsync();
    }
}
