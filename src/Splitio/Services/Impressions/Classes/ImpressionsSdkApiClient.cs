using Newtonsoft.Json;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsSdkApiClient : SdkApiClient, IImpressionsSdkApiClient
    {
        private const string TestImpressionsUrlTemplate = "/api/testImpressions/bulk";
        private const string ImpressionsCountUrlTemplate = "/api/testImpressions/count";
        private const int MaxAttempts = 3;

        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(ImpressionsSdkApiClient));

        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly int _maxBulkSize;

        public ImpressionsSdkApiClient(string apiKey,
            Dictionary<string, string> headers,
            string baseUrl,
            long connectionTimeOut,
            long readTimeout,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            IWrapperAdapter wrapperAdapter,
            int maxBulkSize) : base(apiKey, headers, baseUrl, connectionTimeOut, readTimeout, telemetryRuntimeProducer)
        {
            _wrapperAdapter = wrapperAdapter;
            _maxBulkSize = maxBulkSize;
        }

        public async void SendBulkImpressions(List<KeyImpression> impressions)
        {
            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                if (impressions.Count <= _maxBulkSize)
                {
                    await BuildJsonAndPost(impressions, clock);
                    return;
                }

                while (impressions.Count > 0)
                {
                    var bulkToPost = Util.Helper.TakeFromList(impressions, _maxBulkSize);

                    await BuildJsonAndPost(bulkToPost, clock);
                }
            }
        }

        public async void SendBulkImpressionsCount(List<ImpressionsCountModel> impressionsCount)
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

        public string ConvertToJson(List<ImpressionsCountModel> impressionsCount)
        {
            return JsonConvert.SerializeObject(new { pf = impressionsCount });
        }

        private async Task BuildJsonAndPost(List<KeyImpression> impressions, Util.SplitStopwatch clock)
        {
            var impressionsJson = ConvertToJson(impressions);

            for (int i = 0; i < MaxAttempts; i++)
            {
                if (i > 0) _wrapperAdapter.TaskDelay(500).Wait();                

                var response = await ExecutePost(TestImpressionsUrlTemplate, impressionsJson);

                RecordTelemetry(nameof(SendBulkImpressions), (int)response.statusCode, response.content, ResourceEnum.ImpressionSync, clock);

                if (response.statusCode >= System.Net.HttpStatusCode.OK && response.statusCode < System.Net.HttpStatusCode.Ambiguous)
                {
                    _log.Debug($"Post bulk impressions success in {i} attempts.");
                    return;
                }
            }

            _log.Debug($"Post bulk impressions fail after {MaxAttempts} attempts.");
        }
    }
}
