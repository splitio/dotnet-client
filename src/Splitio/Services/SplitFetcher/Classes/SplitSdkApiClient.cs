using Splitio.CommonLibraries;
using Splitio.Constants;
using Splitio.Domain;
using Splitio.Services.Common;
using Splitio.Services.Filters;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.SplitFetcher.Classes
{
    public class SplitSdkApiClient : ISplitSdkApiClient
    {
        private const int PROXY_CHECK_INTERVAL_MS = 24 * 60 * 60 * 1000;
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitSdkApiClient));

        private readonly ISplitioHttpClient _httpClient;
        private readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;
        private readonly string _baseUrl;
        private readonly string _flagSets;
        private readonly bool _proxy;
        private readonly int _proxyCheckIntervalMs;

        private string _flagSpec = ApiVersions.LatestFlagsSpec;
        private long? _lastProxyCheckTimestamp;

        public SplitSdkApiClient(ISplitioHttpClient httpClient,
            ITelemetryRuntimeProducer telemetryRuntimeProducer,
            string baseUrl,
            IFlagSetsFilter flagSetsFilter,
            bool proxy,
            int? proxyCheckIntervalMs = null)
        {
            _httpClient = httpClient;
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            _baseUrl = baseUrl;
            _flagSets = flagSetsFilter.GetFlagSets();
            _proxy = proxy;
            _proxyCheckIntervalMs = proxyCheckIntervalMs ?? PROXY_CHECK_INTERVAL_MS;
        }

        public async Task<ApiFetchResult> FetchSplitChangesAsync(FetchOptions fetchOptions)
        {
            using (var clock = new Util.SplitStopwatch())
            {
                clock.Start();

                try
                {
                    var requestUri = GetRequestUri(fetchOptions.FeatureFlagsSince, fetchOptions.RuleBasedSegmentsSince, fetchOptions.Till);

                    if (ShouldSwitchToLatestFlagsSpec)
                    {
                        _flagSpec = ApiVersions.LatestFlagsSpec;
                        _log.Info($"Switching to new Feature flag spec {_flagSpec} and fetching.");
                        requestUri = GetRequestUri(-1, -1, fetchOptions.Till);
                    }

                    var response = await _httpClient.GetAsync(requestUri, fetchOptions.CacheControlHeaders);

                    clock.Stop();
                    Util.Helper.RecordTelemetrySync(nameof(FetchSplitChangesAsync), response, ResourceEnum.SplitSync, clock, _telemetryRuntimeProducer, _log);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = new ApiFetchResult
                        {
                            Success = true,
                            Spec = _flagSpec,
                            Content = response.Content
                        };

                        if (_flagSpec != ApiVersions.Spec1_1)
                        {
                            result.ClearCache = _lastProxyCheckTimestamp != null;
                            _lastProxyCheckTimestamp = null;
                        }

                        return result;
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest && _flagSpec.Equals(ApiVersions.LatestFlagsSpec) && _proxy)
                    {
                        _flagSpec = ApiVersions.Spec1_1;
                        _lastProxyCheckTimestamp = CurrentTimeHelper.CurrentTimeMillis();
                        _log.Info($"FetchSplitChange BadRequest: {requestUri}");
                        _log.Warn($"Detected proxy without support for Feature flags spec {ApiVersions.LatestFlagsSpec} version, will switch to spec version {_flagSpec}");

                        return await FetchSplitChangesAsync(fetchOptions);
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.RequestUriTooLong)
                    {
                        _log.Error($"SDK Initialization, the amount of flag sets provided are big causing uri length error.");
                    }
                }
                catch (Exception e)
                {
                    _log.Error("Exception caught executing FetchSplitChanges", e);
                }

                return new ApiFetchResult
                {
                    Success = false,
                    Content = string.Empty
                };
            }
        }

        private string GetRequestUri(long since, long rbSinceTarget, long? till)
        {
            var uri = $"{_baseUrl}/api/splitChanges?s={_flagSpec}&since={since}";

            if (_flagSpec.Equals(ApiVersions.LatestFlagsSpec))
            {
                uri = $"{uri}&rbSince={rbSinceTarget}";
            }

            if (!string.IsNullOrEmpty(_flagSets))
                uri = $"{uri}&sets={_flagSets}";

            if (till.HasValue)
                uri = $"{uri}&till={till.Value}";

            return uri;
        }

        private bool ShouldSwitchToLatestFlagsSpec => _lastProxyCheckTimestamp != null &&
                CurrentTimeHelper.CurrentTimeMillis() - _lastProxyCheckTimestamp >= _proxyCheckIntervalMs &&
                !_flagSpec.Equals(ApiVersions.LatestFlagsSpec);
    }
}
