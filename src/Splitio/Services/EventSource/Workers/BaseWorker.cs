using Splitio.Services.Logger;

namespace Splitio.Services.EventSource.Workers
{
    public abstract class BaseWorker : IWorker
    {
        protected readonly string _name;
        protected readonly ISplitLogger _log;
        protected bool _running;

        public BaseWorker(string name, ISplitLogger log)
        {
            _name = name;
            _log = log;
        }

        public void Start()
        {
            if (_running)
            {
                _log.Debug($"{_name} is already running.");
                return;
            }

            _running = true;
        }

        public void Stop()
        {
            if (!_running)
            {
                _log.Debug($"{_name} is not running.");
                return;
            }

            _running = false;
        }
    }
}
