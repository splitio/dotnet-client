using Splitio.CommonLibraries;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.SplitFetcher.Interfaces;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class SegmentSdkApiClient : SdkApiClient, ISegmentSdkApiClient
    {
        private const string SegmentChangesUrlTemplate = "/api/segmentChanges/{segment_name}";
        private const string UrlParameterSince = "?since=";
        private const string SegmentFetcherTime = "segmentChangeFetcher.time";
        private const string SegmentFetcherStatus = "segmentChangeFetcher.status.{0}";
        private const string SegmentFetcherException = "segmentChangeFetcher.exception";

        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(SegmentSdkApiClient));

        public SegmentSdkApiClient(string apiKey,
            Dictionary<string, string> headers,
            string baseUrl,
            long connectionTimeOut,
            long readTimeout,
            ITelemetryRuntimeProducer telemetryRuntimeProducer) : base(apiKey, headers, baseUrl, connectionTimeOut, readTimeout, telemetryRuntimeProducer)
        { }

        public async Task<string> FetchSegmentChanges(string name, long since)
        {
            var clock = new Stopwatch();
            clock.Start();

            try
            {
                var requestUri = GetRequestUri(name, since);
                var response = await ExecuteGet(requestUri);

                if ((int)response.statusCode >= (int)HttpStatusCode.OK && (int)response.statusCode < (int)HttpStatusCode.Ambiguous)
                {
                    if (_log.IsDebugEnabled)
                    {
                        _log.Debug($"FetchSegmentChanges with name '{name}' took {clock.ElapsedMilliseconds} milliseconds using uri '{requestUri}'");
                    }

                    _telemetryRuntimeProducer.RecordSuccessfulSync(ResourceEnum.SegmentSync, CurrentTimeHelper.CurrentTimeMillis());

                    return response.content;
                }

                _log.Error(response.statusCode == HttpStatusCode.Forbidden
                    ? "factory instantiation: you passed a browser type api_key, please grab an api key from the Split console that is of type sdk"
                    : $"Http status executing FetchSegmentChanges: {response.statusCode.ToString()} - {response.content}");

                _telemetryRuntimeProducer.RecordSyncError(ResourceEnum.SegmentSync, (int)response.statusCode);

                return string.Empty;
               
            }
            catch (Exception e)
            {
                _log.Error("Exception caught executing FetchSegmentChanges", e);

                return string.Empty;
            }
        }

        private string GetRequestUri(string name, long since)
        {
            var segmentChangesUrl = SegmentChangesUrlTemplate.Replace("{segment_name}", name);

            return string.Concat(segmentChangesUrl, UrlParameterSince, Uri.EscapeDataString(since.ToString()));
        }
    }
}
