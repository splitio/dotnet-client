using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;

namespace Splitio.Services.Shared.Classes
{
    public class SelfRefreshingBlockUntilReadyService : IBlockUntilReadyService
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SelfRefreshingBlockUntilReadyService));

        private readonly IStatusManager _statusManager;
        private readonly ITelemetryInitProducer _telemetryInitProducer;
        private readonly IEventsManager<SdkEvent, SdkInternalEvent, EventMetadata> _eventsManager;

        public SelfRefreshingBlockUntilReadyService(IStatusManager statusManager, ITelemetryInitProducer telemetryInitProducer,
            IEventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager)
        {
            _statusManager = statusManager;
            _telemetryInitProducer = telemetryInitProducer;
            _eventsManager = eventsManager;
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
                _eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkTimedOut,
                    new EventMetadata(new Dictionary<string, object>()));
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
    }
}
