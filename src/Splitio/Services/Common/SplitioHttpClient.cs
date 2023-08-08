using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public class SplitioHttpClient : ISplitioHttpClient
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(SplitioHttpClient));
        
        private readonly HttpClient _httpClient;

        private bool _disposed;

        public SplitioHttpClient(string apiKey,
            SelfRefreshingConfig config,
            Dictionary<string, string> headers)
        {
#if NET45
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)12288 | (SecurityProtocolType)3072;
#endif
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };

            if (!string.IsNullOrEmpty(config.ProxyHost))
            {
                handler.Proxy = new WebProxy(config.ProxyHost, config.ProxyPort);
            }

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(config.HttpConnectionTimeout + config.HttpReadTimeout)
            };

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.Http.Bearer, apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.Http.MediaTypeJson));

            foreach (var header in headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        public async Task<HTTPResult> GetAsync(string url, bool cacheControlHeadersEnabled = false)
        {
            var result = new HTTPResult();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };

            if (cacheControlHeadersEnabled)
                request.Headers.Add(Constants.Http.CacheControlKey, Constants.Http.CacheControlValue);

            try
            {
                using (var response = await _httpClient.SendAsync(request))
                {
                    result.StatusCode = response.StatusCode;
                    result.Content = await response.Content.ReadAsStringAsync();
                    result.IsSuccessStatusCode = response.IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                _log.Error($"Exception caught executing GET {url}", e);
            }

            return result;
        }

        public Task<HttpResponseMessage> GetAsync(string url, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return _httpClient.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        public async Task<HTTPResult> PostAsync(string url, string data)
        {
            var result = new HTTPResult();

            try
            {
                using (var response = await _httpClient.PostAsync(new Uri(url), new StringContent(data, Encoding.UTF8, "application/json")))
                {
                    result.StatusCode = response.StatusCode;
                    result.Content = await response.Content.ReadAsStringAsync();
                    result.IsSuccessStatusCode = response.IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                _log.Error(string.Format("Exception caught executing POST {0}", url), e);
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _httpClient.CancelPendingRequests();
            _httpClient.Dispose();

            _disposed = true;
        }
    }
}
