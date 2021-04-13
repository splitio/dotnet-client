using Newtonsoft.Json;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsSdkApiClient : SdkApiClient, IImpressionsSdkApiClient
    {
        private const string TestImpressionsUrlTemplate = "/api/testImpressions/bulk";
        private const string ImpressionsCountUrlTemplate = "/api/testImpressions/count";

        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(ImpressionsSdkApiClient));

        public ImpressionsSdkApiClient(string apiKey,
            Dictionary<string, string> headers,
            string baseUrl,
            long connectionTimeOut,
            long readTimeout,
            ITelemetryRuntimeProducer telemetryRuntimeProducer) : base(apiKey, headers, baseUrl, connectionTimeOut, readTimeout, telemetryRuntimeProducer)
        { }

        public async void SendBulkImpressions(List<KeyImpression> impressions)
        {
            var impressionsJson = ConvertToJson(impressions);

            var response = await ExecutePost(TestImpressionsUrlTemplate, impressionsJson);

            if ((int)response.statusCode < (int)HttpStatusCode.OK || (int)response.statusCode >= (int)HttpStatusCode.Ambiguous)
            {
                _log.Error($"Http status executing SendBulkImpressions: {response.statusCode.ToString()} - {response.content}");

                _telemetryRuntimeProducer.RecordSyncError(ResourceEnum.ImpressionSync, (int)response.statusCode);
            }
        }

        public async void SendBulkImpressionsCount(ConcurrentDictionary<KeyCache, int> impressionsCount)
        {
            var json = ConvertToJson(impressionsCount);

            var response = await ExecutePost(ImpressionsCountUrlTemplate, json);

            if ((int)response.statusCode < (int)HttpStatusCode.OK || (int)response.statusCode >= (int)HttpStatusCode.Ambiguous)
            {
                _log.Error($"Http status executing SendBulkImpressionsCount: {response.statusCode.ToString()} - {response.content}");

                _telemetryRuntimeProducer.RecordSyncError(ResourceEnum.ImpressionCountSync, (int)response.statusCode);
            }
        }

        // Public for tests
        public string ConvertToJson(List<KeyImpression> impressions)
        {
            var impressionsPerFeature =
                impressions
                .GroupBy(item => item.feature)
                .Select(group => new { f = group.Key, i = group.Select(x => new { k = x.keyName, t = x.treatment, m = x.time, c = x.changeNumber, r = x.label, b = x.bucketingKey }) });

            return JsonConvert.SerializeObject(impressionsPerFeature);
        }

        public string ConvertToJson(ConcurrentDictionary<KeyCache, int> impressionsCount)
        {
            return JsonConvert.SerializeObject(new
            {
                pf = impressionsCount.Select(item => new
                {
                    f = item.Key.SplitName,
                    m = item.Key.TimeFrame,
                    rc = item.Value
                })
            });
        }
    }
}
