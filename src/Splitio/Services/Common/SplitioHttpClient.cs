using Splitio.CommonLibraries;
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
        private readonly ISplitLogger _log;
        private readonly HttpClient _httpClient;

        public SplitioHttpClient(Dictionary<string, string> headers)
        {
            _httpClient = new HttpClient();

            foreach (var header in headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        public SplitioHttpClient(string apiKey,
            long connectionTimeOut,
            Dictionary<string, string> headers = null)
        {
#if NET45 || NET461
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)Constants.Http.ProtocolTypeTls12;
#endif
            _log = WrapperAdapter.GetLogger(typeof(SplitioHttpClient));
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMilliseconds(connectionTimeOut)
            };

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.Http.Bearer, apiKey);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
        }

        public async Task<HTTPResult> GetAsync(string url)
        {
            var result = new HTTPResult();

            try
            {
                using (var response = await _httpClient.GetAsync(new Uri(url)))
                {
                    result.statusCode = response.StatusCode;
                    result.content = await response.Content.ReadAsStringAsync();
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

        public void Dispose()
        {
            _httpClient.CancelPendingRequests();
            _httpClient.Dispose();
        }

        public async Task<HTTPResult> PostAsync(string url, string data)
        {
            var result = new HTTPResult();

            try
            {
                using (var response = await _httpClient.PostAsync(new Uri(url), new StringContent(data, Encoding.UTF8, "application/json")))
                {
                    result.statusCode = response.StatusCode;
                    result.content = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception e)
            {
                _log.Error(string.Format("Exception caught executing POST {0}", url), e);
            }

            return result;
        }
    }
}
