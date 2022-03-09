using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Threading;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsCountSender : IImpressionsCountSender
    {
        protected static readonly ISplitLogger Logger = WrapperAdapter.GetLogger(typeof(ImpressionsCountSender));

        private readonly IImpressionsSenderAdapter _senderAdapter;
        private readonly IImpressionsCounter _impressionsCounter;
        private readonly ITasksManager _tasksManager;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly int _interval;
        private readonly object _lock = new object();

        private bool _running;

        public ImpressionsCountSender(IImpressionsSenderAdapter senderAdapter,
            IImpressionsCounter impressionsCounter,
            ITasksManager tasksManager,
            int interval)
        {
            _senderAdapter = senderAdapter;
            _impressionsCounter = impressionsCounter;            
            _cancellationTokenSource = new CancellationTokenSource();
            _interval = interval;
            _running = false;
            _tasksManager = tasksManager;
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_running) return;

                _running = true;
                _tasksManager.StartPeriodic(() => SendBulkImpressionsCount(), _interval * 1000, _cancellationTokenSource, "Main Impressions Count Sender.");
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_running) return;

                _running = false;
                _cancellationTokenSource.Cancel();
                SendBulkImpressionsCount();
            }
        }

        private void SendBulkImpressionsCount()
        {
            var impressions = _impressionsCounter.PopAll();

            if (impressions.Count > 0)
            {
                try
                {
                    _senderAdapter.RecordImpressionsCount(impressions);
                }
                catch (Exception e)
                {
                    Logger.Error("Exception caught sending impressions count.", e);
                }
            }
        }
    }
}
