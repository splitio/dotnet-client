using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Telemetry.Domain.Enums;
using Splitio.Telemetry.Storages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Splitio.CommonLibraries
{
    public class SdkApiClient : ISdkApiClient
    {
        private static readonly ISplitLogger _log = WrapperAdapter.GetLogger(typeof(SdkApiClient));

        private readonly HttpClient _httpClient;
        protected readonly ITelemetryRuntimeProducer _telemetryRuntimeProducer;

        public SdkApiClient (string apiKey,
            Dictionary<string, string> headers,
            string baseUrl,
            long connectionTimeOut,
            long readTimeout,
            ITelemetryRuntimeProducer telemetryRuntimeProducer)
        {
#if NET45
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)12288 | (SecurityProtocolType)3072;
#endif
            _telemetryRuntimeProducer = telemetryRuntimeProducer;
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                //TODO: find a way to store it in sepparated parameters
                Timeout = TimeSpan.FromMilliseconds(connectionTimeOut + readTimeout)
            };

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.Http.Bearer, apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.Http.MediaTypeJson));

            foreach (var header in headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        public virtual async Task<HTTPResult> ExecuteGet(string requestUri, bool cacheControlHeadersEnabled = false)
        {
            var result = new HTTPResult();

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(_httpClient.BaseAddress, requestUri),
                Method = HttpMethod.Get
            };

            if (cacheControlHeadersEnabled) request.Headers.Add(Constants.Http.CacheControlKey, Constants.Http.CacheControlValue);

            try
            {
                using (var response = await _httpClient.SendAsync(request))
                {
                    result.statusCode = response.StatusCode;
                    result.content = await response.Content.ReadAsStringAsync();
                }
            }
            catch(Exception e)
            {
                _log.Error(string.Format("Exception caught executing GET {0}", requestUri), e);
            }

            return result;
        }

        public virtual async Task<HTTPResult> ExecutePost(string requestUri, string data)
        {
            var result = new HTTPResult();

            try
            {
                using (var response = await _httpClient.PostAsync(requestUri, new StringContent(data, Encoding.UTF8, "application/json")))
                {
                    result.statusCode = response.StatusCode;
                    result.content = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception e)
            {
                _log.Error(string.Format("Exception caught executing POST {0}", requestUri), e);
            }

            return result;
        }

        protected void RecordTelemetry(string method, int statusCode, string content, ResourceEnum resource, Util.SplitStopwatch clock)
        {
            if (statusCode >= (int)HttpStatusCode.OK && statusCode < (int)HttpStatusCode.Ambiguous)
            {
                _telemetryRuntimeProducer.RecordSyncLatency(resource, Util.Metrics.Bucket(clock.ElapsedMilliseconds));
                _telemetryRuntimeProducer.RecordSuccessfulSync(resource, CurrentTimeHelper.CurrentTimeMillis());
            }
            else
            {
                _log.Error($"Http status executing {method}: {statusCode.ToString()} - {content}");

                _telemetryRuntimeProducer.RecordSyncError(resource, statusCode);
            }
        }
    }
}
