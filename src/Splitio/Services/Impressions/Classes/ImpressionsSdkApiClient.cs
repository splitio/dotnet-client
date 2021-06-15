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
using System.Diagnostics;
using System.Linq;

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
            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                var impressionsJson = ConvertToJson(impressions);

                var response = await ExecutePost(TestImpressionsUrlTemplate, impressionsJson);

                RecordTelemetry(nameof(SendBulkImpressions), (int)response.statusCode, response.content, ResourceEnum.ImpressionSync, clock);
            }
        }

        public async void SendBulkImpressionsCount(ConcurrentDictionary<KeyCache, int> impressionsCount)
        {
            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                var json = ConvertToJson(impressionsCount);

                var response = await ExecutePost(ImpressionsCountUrlTemplate, json);

                RecordTelemetry(nameof(SendBulkImpressionsCount), (int)response.statusCode, response.content, ResourceEnum.ImpressionCountSync, clock);
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
