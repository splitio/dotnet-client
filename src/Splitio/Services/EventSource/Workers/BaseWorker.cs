using Splitio.Services.Logger;
using Splitio.Services.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.EventSource.Workers
{
    public abstract class BaseWorker : IWorker
    {
        protected readonly string _name;
        protected readonly ISplitTask _task;
        protected readonly ISplitLogger _log;

        protected CancellationTokenSource _cancellationTokenSource;

        public BaseWorker(string name, ISplitLogger log, ISplitTask task)
        {
            _name = name;
            _log = log;
            _task = task;
            _task.SetAction(async () => await ExecuteAsync());
        }

        public void Start()
        {
            try
            {
                if (_task.IsRunning())
                {
                    _log.Debug($"{_name} already running.");
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _task.Start();
            }
            catch (Exception ex)
            {
                _log.Debug($"{_name}.Start threw an Exception", ex);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (!_task.IsRunning())
                {
                    _log.Debug($"{_name} not running.");
                    return;
                }
                await _task.StopAsync();

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception ex)
            {
                _log.Debug($"{_name}.Stop threw an Exception: {ex.Message}");
            }
        }

        protected abstract Task ExecuteAsync();
    }
}
