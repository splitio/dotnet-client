using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.Tasks;
using Splitio.Telemetry.Storages;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Classes
{
    public class SelfRefreshingBlockUntilReadyService : IBlockUntilReadyService
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SelfRefreshingBlockUntilReadyService));

        private readonly IStatusManager _statusManager;
        private readonly IInternalEventsTask _internalEventsTask;
        private readonly ITelemetryInitProducer _telemetryInitProducer;

        public SelfRefreshingBlockUntilReadyService(IStatusManager statusManager, ITelemetryInitProducer telemetryInitProducer,
            IInternalEventsTask internalEventsTask)
        {
            _statusManager = statusManager;
            _telemetryInitProducer = telemetryInitProducer;
            _internalEventsTask = internalEventsTask;
        }

        public void BlockUntilReady(int blockMilisecondsUntilReady)
        {
            if (IsSdkReady()) return;
            
            if (blockMilisecondsUntilReady <= 0)
            {
                _log.Warn("The blockMilisecondsUntilReady param has to be higher than 0.");
            }
                
            if (!_statusManager.WaitUntilReady(blockMilisecondsUntilReady))
            {
                _internalEventsTask.AddToQueue(SdkInternalEvent.SdkTimedOut, null).ContinueWith(OnAddToQueueFailed, TaskContinuationOptions.OnlyOnFaulted);
                _telemetryInitProducer.RecordBURTimeout();
                throw new TimeoutException($"SDK was not ready in {blockMilisecondsUntilReady} milliseconds");
            }
        }

        public bool IsSdkReady()
        {
            try
            {
                return _statusManager.IsReady();
            }
            catch (Exception ex)
            {
                _log.Error("Somenthing went wrong in checking if the sdk is ready.", ex);
                return false;
            }
        }

        public void OnAddToQueueFailed(Task task)
        {
            _log.Error($"Failed to add internal event to queue: {task.Exception.Message}");
        }
    }
}
