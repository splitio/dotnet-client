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

namespace Splitio.Services.SplitFetcher.Classes
{
    public class SplitSdkApiClient : SdkApiClient, ISplitSdkApiClient
    {
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(SplitSdkApiClient));

        private const string SplitChangesUrlTemplate = "/api/splitChanges";
        private const string UrlParameterSince = "?since=";
        private const string SplitFetcherTime = "splitChangeFetcher.time";
        private const string SplitFetcherStatus = "splitChangeFetcher.status.{0}";
        private const string SplitFetcherException = "splitChangeFetcher.exception";        

        public SplitSdkApiClient(string apiKey,
            Dictionary<string, string> headers,
            string baseUrl,
            long connectionTimeOut,
            long readTimeout,
            ITelemetryRuntimeProducer telemetryRuntimeProducer) : base(apiKey, headers, baseUrl, connectionTimeOut, readTimeout, telemetryRuntimeProducer)
        { }

        public async Task<string> FetchSplitChanges(long since, bool cacheControlHeaders = false)
        {
            var clock = new Stopwatch();
            clock.Start();

            try
            {
                var requestUri = GetRequestUri(since);
                var response = await ExecuteGet(requestUri, cacheControlHeaders);

                if ((int)response.statusCode >= (int)HttpStatusCode.OK && (int)response.statusCode < (int)HttpStatusCode.Ambiguous)
                {
                    _telemetryRuntimeProducer.RecordSyncLatency(ResourceEnum.SplitSync, Util.Metrics.Bucket(clock.ElapsedMilliseconds));
                    _telemetryRuntimeProducer.RecordSuccessfulSync(ResourceEnum.SplitSync, CurrentTimeHelper.CurrentTimeMillis());                    

                    return response.content;
                }

                _log.Error($"Http status executing FetchSplitChanges: {response.statusCode.ToString()} - {response.content}");

                _telemetryRuntimeProducer.RecordSyncError(ResourceEnum.SplitSync, (int)response.statusCode);

                return string.Empty;
            }
            catch (Exception e)
            {
                _log.Error("Exception caught executing FetchSplitChanges", e);

                return string.Empty;
            }
        }

        private string GetRequestUri(long since)
        {
            return string.Concat(SplitChangesUrlTemplate, UrlParameterSince, Uri.EscapeDataString(since.ToString()));
        }
    }
}
