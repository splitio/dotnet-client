using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class SegmentSdkApiClient : ISegmentSdkApiClient
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SegmentSdkApiClient));

        private readonly ISplitioHttpClient _httpClient;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly string _baseUrl;

        public SegmentSdkApiClient(ISplitioHttpClient httpClient,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            string baseUrl)
        {
            _httpClient = httpClient;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _baseUrl = baseUrl;
        }

        public async Task<string> FetchSegmentChanges(string name, long since, FetchOptions fetchOptions)
        {
            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                try
                {
                    var requestUri = GetRequestUri(name, since, fetchOptions.Till);
                    var response = await _httpClient.GetAsync(requestUri, fetchOptions.CacheControlHeaders);

                    Util.Helper.RecordTelemetrySync(nameof(FetchSegmentChanges), response.statusCode, response.content, ResourceEnum.SegmentSync, clock, _telemetryRuntimeProducer, _log);

                    if (response.statusCode >= HttpStatusCode.OK && response.statusCode < HttpStatusCode.Ambiguous)
                    {
                        _log.Debug($"FetchSegmentChanges with name '{name}' took {clock.ElapsedMilliseconds} milliseconds using uri '{requestUri}'");

                        return response.content;
                    }
                    else if (response.statusCode == HttpStatusCode.Forbidden)
                    {
                        _log.Error("factory instantiation: you passed a browser type api_key, please grab an api key from the Split console that is of type sdk");
                    }

                    return string.Empty;

                }
                catch (Exception e)
                {
                    _log.Error("Exception caught executing FetchSegmentChanges", e);

                    return string.Empty;
                }
            }
        }

        private string GetRequestUri(string name, long since, long? till = null)
        {
            var uri = $"{_baseUrl}/api/segmentChanges/{name}?since={Uri.EscapeDataString(since.ToString())}";

            if (till.HasValue)
                return $"{uri}&till={Uri.EscapeDataString(till.Value.ToString())}";

            return uri;
        }
    }
}
