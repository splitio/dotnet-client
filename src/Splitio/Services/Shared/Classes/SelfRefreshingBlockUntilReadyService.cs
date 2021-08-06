﻿using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Storages;
using System;

namespace Splitio.Services.Shared.Classes
{
    public class SelfRefreshingBlockUntilReadyService : IBlockUntilReadyService
    {
        private readonly IReadinessGatesCache _gates;
        private readonly ISplitLogger _log;
        private readonly ITelemetryInitProducer _telemetryInitProducer;

        public SelfRefreshingBlockUntilReadyService(IReadinessGatesCache gates,
            ITelemetryInitProducer telemetryInitProducer,
            ISplitLogger log = null)
        {
            _gates = gates;
            _telemetryInitProducer = telemetryInitProducer;
            _log = log ?? WrapperAdapter.GetLogger(typeof(SelfRefreshingBlockUntilReadyService));
        }

        public void BlockUntilReady(int blockMilisecondsUntilReady)
        {
            if (!IsSdkReady())
            {
                if (blockMilisecondsUntilReady <= 0)
                {
                    _log.Warn("The blockMilisecondsUntilReady param has to be higher than 0.");
                }
                
                if (!_gates.IsSDKReady(blockMilisecondsUntilReady))
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
                return _gates.IsSDKReady(0);
            }
            catch (Exception ex)
            {
                _log.Error("Somenthing went wrong in checking if the sdk is ready.", ex);
                return false;
            }
        }
    }
}
