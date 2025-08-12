using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.Impressions.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Shared.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionsSdkApiClient : IImpressionsSdkApiClient
    {
        private const int MaxAttempts = 3;

        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(ImpressionsSdkApiClient));

        private readonly ISplitioHttpClient _httpClient;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly IWrapperAdapter _wrapperAdapter;
        private readonly string _baseUrl;
        private readonly int _maxBulkSize;

        public ImpressionsSdkApiClient(ISplitioHttpClient httpClient,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            string baseUrl,
            IWrapperAdapter wrapperAdapter,
            int maxBulkSize)
        {
            _httpClient = httpClient;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _baseUrl = baseUrl;
            _wrapperAdapter = wrapperAdapter;
            _maxBulkSize = maxBulkSize;
        }

        public async Task SendBulkImpressionsAsync(List<KeyImpression> impressions)
        {
            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                if (impressions.Count <= _maxBulkSize)
                {
                    await BuildJsonAndPostAsync(impressions, clock);
                    return;
                }

                while (impressions.Count > 0)
                {
                    var bulkToPost = Util.Helper.TakeFromList(impressions, _maxBulkSize);

                    await BuildJsonAndPostAsync(bulkToPost, clock);
                }
            }
        }

        public async Task SendBulkImpressionsCountAsync(List<ImpressionsCountModel> impressionsCount)
        {
            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                var json = ConvertToJson(impressionsCount);

                var response = await _httpClient.PostAsync(ImpressionsCountUrl, json);

                Util.Helper.RecordTelemetrySync(nameof(SendBulkImpressionsCountAsync), response, ResourceEnum.ImpressionCountSync, clock, _telemetryRuntimeProducer, _log);
            }
        }

        // Public for tests
        public static string ConvertToJson(List<KeyImpression> impressions)
        {
            List<Dictionary<String, Object>> impressionsPerFeature = new List<Dictionary<String, Object>>();
            List<Dictionary<String, Object>> currentFeatureImps = new List<Dictionary<String, Object>>();
            String currentFeature = "";
            Dictionary<String, Object> impRecord = new Dictionary<String, Object>();
            Boolean first = true;
            foreach (KeyImpression impression in impressions.OrderBy(i => i.Feature))
            {
                if (first)
                {
                    currentFeature = impression.Feature;
                    first = false;
                }

                if (impression.Feature != currentFeature)
                {
                    impressionsPerFeature.Add(new Dictionary<String, Object>
                    {
                        { "f", currentFeature },
                        { "i", currentFeatureImps }
                    });
                    currentFeature = "";
                    currentFeatureImps = new List<Dictionary<String, Object>>();
                }
                impRecord = new Dictionary<String, Object>
                {
                    { "k", impression.KeyName },
                    { "t", impression.Treatment },
                    { "m", impression.Time },
                    { "c", impression.ChangeNumber },
                    { "r", impression.Label },
                    { "b", impression.BucketingKey }
                };
                if (impression.Properties != null)
                {
                    impRecord.Add("properties", impression.Properties);
                }
                currentFeatureImps.Add(impRecord);
                currentFeature = impression.Feature;
            }
            impressionsPerFeature.Add(new Dictionary<String, Object>
            {
                { "f", currentFeature },
                { "i", currentFeatureImps }
            });
          

                return JsonConvert.SerializeObject(impressionsPerFeature);
        }

        public static string ConvertToJson(List<ImpressionsCountModel> impressionsCount)
        {
            return JsonConvert.SerializeObject(new { pf = impressionsCount });
        }

        private async Task BuildJsonAndPostAsync(List<KeyImpression> impressions, Util.SplitStopwatch clock)
        {
            var impressionsJson = ConvertToJson(impressions);

            for (int i = 0; i < MaxAttempts; i++)
            {
                if (i > 0) await Task.Delay(500);

                var response = await _httpClient.PostAsync(TestImpressionsUrl, impressionsJson);

                Util.Helper.RecordTelemetrySync(nameof(SendBulkImpressionsAsync), response, ResourceEnum.ImpressionSync, clock, _telemetryRuntimeProducer, _log);

                if (response.IsSuccessStatusCode)
                {
                    _log.Debug($"Post bulk impressions success in {i} attempts.");
                    return;
                }
            }

            _log.Debug($"Post bulk impressions fail after {MaxAttempts} attempts.");
        }

        private string TestImpressionsUrl => $"{_baseUrl}/api/testImpressions/bulk";
        private string ImpressionsCountUrl => $"{_baseUrl}/api/testImpressions/count";
    }
}
