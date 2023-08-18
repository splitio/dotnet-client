using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Classes
{
    public class SplitSdkApiClient : ISplitSdkApiClient
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitSdkApiClient));

        private readonly ISplitioHttpClient _httpClient;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly string _baseUrl;
        private readonly bool _withFlagSets;
        private readonly List<string> _flagSets;

        public SplitSdkApiClient(ISplitioHttpClient httpClient,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            string baseUrl,
            HashSet<string> flagSets)
        {
            _httpClient = httpClient;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _baseUrl = baseUrl;
            _withFlagSets = flagSets.Any();
            _flagSets = flagSets
                .OrderBy(fs => fs)
                .ToList();
        }

        public async Task<string> FetchSplitChanges(long since, FetchOptions fetchOptions)
        {
            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                try
                {
                    var requestUri = GetRequestUri(since, fetchOptions.Till);                    
                    var response = await _httpClient.GetAsync(requestUri, fetchOptions.CacheControlHeaders);

                    Util.Helper.RecordTelemetrySync(nameof(FetchSplitChanges), response, ResourceEnum.SplitSync, clock, _telemetryRuntimeProducer, _log);

                    if (response.IsSuccessStatusCode)
                    {
                        return response.Content;
                    }

                    return string.Empty;
                }
                catch (Exception e)
                {
                    _log.Error("Exception caught executing FetchSplitChanges", e);

                    return string.Empty;
                }
            }
        }

        private string GetRequestUri(long since, long? till = null)
        {
            var uri = $"{_baseUrl}/api/splitChanges?since={Uri.EscapeDataString(since.ToString())}";

            if (till.HasValue)
                uri = $"{uri}&till={Uri.EscapeDataString(till.Value.ToString())}";

            //if (_withFlagSets)
            //    uri = $"{uri}&sets={string.Join(",", _flagSets)}";

            return uri;
        }
    }
}
