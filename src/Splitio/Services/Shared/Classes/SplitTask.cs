using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using System;
using System.Timers;

namespace Splitio.Services.Shared.Classes
{
    public class SplitTask : ISplitTask
    {
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitTask));

        private readonly IStatusManager _statusManager;
        private readonly Timer _timer;
        private readonly string _name;

        public SplitTask(Enums.Task task, IStatusManager statusManager)
        {
            _timer = new Timer();
            _name = task.ToString();
            _statusManager = statusManager;
        }

        public SplitTask(Enums.Task task, double interval, IStatusManager statusManager, bool repeat = true)
        {
            _timer = new Timer(interval) { AutoReset = repeat };
            _name = task.ToString();
            _statusManager = statusManager;
        }

        public void Start()
        {
            if (_statusManager.IsDestroyed()) return;

            if (IsRunning())
            {
                _log.Debug($"Periodic Task {_name} already running.");
                return;
            }

            _timer.Start();
            _log.Debug($"Periodic Task started: {_name}. Interval: {_timer.Interval} milliseconds.");
        }

        public void Stop()
        {
            if (!IsRunning()) return;

            _timer.Stop();
            _log.Debug($"Periodic Task stopped: {_name}.");
        }

        public void Kill()
        {
            _timer.Close();
            _log.Debug($"Periodic Task killed: {_name}.");
        }

        public void SetInterval(double interval)
        {
            _timer.Interval = interval;
        }

        public bool IsRunning() => _timer.Enabled;

        public void SetEventHandler(ElapsedEventHandler elapsed)
        {
            _timer.Elapsed += elapsed;
        }
    }
}
