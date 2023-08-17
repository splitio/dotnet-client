﻿using Splitio.Services.Cache.Interfaces;
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
        protected readonly IStatusManager _statusManager;
        
        protected CancellationTokenSource _cts;
        protected Action _action;
        protected Task _task;
        protected int _interval;
        protected bool _running;

        public SplitTask(Enums.Task taskName, int interval, IStatusManager statusManager)
        {
            _taskName = taskName;
            _interval = interval;
            _statusManager = statusManager;
        }

        public void Start()
        {
            if (_statusManager.IsDestroyed())
            {
                _log.Debug($"Task initialization {_taskName}: sdk is already destroyed.");
                return;
            }

            if (IsRunning())
            {
                _log.Debug($"Task {_taskName} already running.");
                return;
            }

            if (_action == null)
            {
                _log.Warn($"Task {_taskName} has not set the action work to excecute.");
                return;
            }

            _running = true;
            _cts = new CancellationTokenSource();
            _task = Task.Factory.StartNew(DoWorkAsync, _cts.Token);

            _log.Debug($"Task {_taskName} running.");
        }

        public async Task StopAsync()
        {
            if (!IsRunning()) return;

            _cts.Cancel();
            if (_task != null) await _task;
            _running = false;
            _cts.Dispose();
        }

        public bool IsRunning()
        {
            return _running;
        }

        public void SetAction(Action action)
        {
            _action = action;
        }

        public void SetInterval(int interval)
        {
            _interval = interval;
        }

        protected abstract Task DoWorkAsync();
    }
}
