using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Splitio.Services.Impressions.Classes
{
    public class InMemoryUniqueKeysSenderAdapter : IUniqueKeysSenderAdapter
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.GetLogger(typeof(InMemoryUniqueKeysSenderAdapter));

        private readonly ITelemetryAPI _telemetryApi;

        public InMemoryUniqueKeysSenderAdapter(ITelemetryAPI telemetryApi)
        {
            _telemetryApi = telemetryApi;
        }

        public void RecordUniqueKeys(ConcurrentDictionary<string, HashSet<string>> uniques)
        {
            try
            {
                _telemetryApi.RecordUniqueKeys(new UniqueKeys(uniques));
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught recording unique keys.", ex);
            }
        }
    }
}
