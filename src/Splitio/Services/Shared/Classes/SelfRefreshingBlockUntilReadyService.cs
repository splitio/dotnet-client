﻿using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Storages;
using System;

namespace Splitio.Services.Shared.Classes
{
    public class SelfRefreshingBlockUntilReadyService : IBlockUntilReadyService
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SelfRefreshingBlockUntilReadyService));

        private readonly IStatusManager _statusManager;
        private readonly ITelemetryInitProducer _telemetryInitProducer;

        public SelfRefreshingBlockUntilReadyService(IStatusManager statusManager, ITelemetryInitProducer telemetryInitProducer)
        {
            _statusManager = statusManager;
            _telemetryInitProducer = telemetryInitProducer;
        }

        public void BlockUntilReady(int blockMilisecondsUntilReady)
        {
            if (!IsSdkReady())
            {
                if (blockMilisecondsUntilReady <= 0)
                {
                    _log.Warn("The blockMilisecondsUntilReady param has to be higher than 0.");
                }
                
                if (!_statusManager.WaitUntilReady(blockMilisecondsUntilReady))
                {
                    _telemetryInitProducer.RecordBURTimeout();
                    throw new TimeoutException(string.Format($"SDK was not ready in {blockMilisecondsUntilReady} miliseconds"));
                }
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
