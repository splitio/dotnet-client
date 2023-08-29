using Splitio.Services.Cache.Interfaces;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    internal class SplitOneTimeTask : SplitTask
    {
        public SplitOneTimeTask(IStatusManager statusManager, Enums.Task taskName) : base(taskName, 0, statusManager)
        { 
        }
        public SplitOneTimeTask(IStatusManager statusManager, Enums.Task taskName, int interval) : base(taskName, interval, statusManager)
        {
        }

        protected override async Task DoWorkAsync()
        {
            try
            {
                if (_interval > 0)
                    await Task.Delay(_interval, _cts.Token);

                if (_function != null)
                    await _function.Invoke();
                else
                    _action.Invoke();

                Stop();
            }
            catch (OperationCanceledException)
            {
                _log.Debug($"Task {_taskName} stopped.");
            }
            catch (Exception ex)
            {
                _log.Trace($"SplitOneTimeTask throw an Exception, Task: {_taskName}.", ex);
            }
        }
    }
}
