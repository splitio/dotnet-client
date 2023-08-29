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

        protected CancellationTokenSource _cts;

        public BaseWorker(string name, ISplitLogger log, ISplitTask task)
        {
            _name = name;
            _log = log;
            _task = task;
            _task.SetFunction(async () => await ExecuteAsync());
        }

        public void Start()
        {
            try
            {
                _cts = new CancellationTokenSource();
                _task.Start();
            }
            catch (Exception ex)
            {
                _log.Debug($"{_name}.Start threw an Exception", ex);
            }
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                _task.Stop();
                _cts?.Dispose();
            }
            catch (Exception ex)
            {
                _log.Debug($"{_name}.Stop threw an Exception: {ex.Message}");
            }
        }

        protected abstract Task ExecuteAsync();
    }
}
