using System;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    internal class SplitPeriodicTask : SplitTask
    {
        public SplitPeriodicTask(Enums.Task taskName, int interval) : base(taskName, interval)
        {
        }

        protected override async Task DoWorkAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    await Task.Delay(_interval, _cts.Token);
                    _action.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                _log.Debug($"Task {_taskName} stopped.");
            }
            catch (Exception ex)
            {
                _log.Trace($"SplitPeriodicTask throw an Exception, Task: {_taskName}.", ex);
            }
        }
    }
}
