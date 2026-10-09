using Splitio.Commons.api.Interfaces;
using Splitio.Commons.Shared.Logger;
using Splitio.Commons.Shared.Utils;
using Splitio.Commons.Domain;
using Splitio.Commons.Telemetry.Domain;
using Splitio.Services.Impressions.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public async Task RecordUniqueKeysAsync(List<Mtks> uniques)
        {
            try
            {
                await _telemetryApi.RecordUniqueKeysAsync(new UniqueKeys(uniques));
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught recording unique keys.", ex);
            }
        }

        public async Task RecordImpressionsCountAsync(List<ImpressionsCountModel> values)
        {
            try
            {
                await _impressionsSdkApiClient.SendBulkImpressionsCountAsync(values);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception caught sending bulk of impressions count.", ex);
            }
        }
    }
}
