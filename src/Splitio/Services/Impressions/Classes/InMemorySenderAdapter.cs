using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Common;
using Splitio.Telemetry.Domain;
using System;
using System.Collections.Generic;

namespace Splitio.Services.Impressions.Classes
{
    public class InMemorySenderAdapter : IImpressionsSenderAdapter
    {
        private static readonly ISplitLogger _logger = WrapperAdapter.Instance().GetLogger(typeof(InMemorySenderAdapter));

        private readonly ITelemetryAPI _telemetryApi;
        private readonly IImpressionsSdkApiClient _impressionsSdkApiClient;

        public InMemorySenderAdapter(ITelemetryAPI telemetryApi,
            IImpressionsSdkApiClient impressionsSdkApiClient)
        {
            _telemetryApi = telemetryApi;
            _impressionsSdkApiClient = impressionsSdkApiClient;
        }

        public void RecordUniqueKeys(List<Mtks> uniques)
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

        public void RecordImpressionsCount(List<ImpressionsCountModel> values)
        {
            try
            {
                _impressionsSdkApiClient.SendBulkImpressionsCount(values);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught sending bulk of impressions count.", ex);
            }
        }
    }
}
