using System;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    internal class SplitOneTimeTask : SplitTask
    {
        public SplitOneTimeTask(Enums.Task taskName) : base(taskName, 0)
        { 
        }
        public SplitOneTimeTask(Enums.Task taskName, int interval) : base(taskName, interval)
        {
        }

        protected override async Task DoWorkAsync()
        {
            try
            {
                if (_interval > 0)
                    await Task.Delay(_interval, _cts.Token);

                _action.Invoke();
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
