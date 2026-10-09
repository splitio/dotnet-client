using Splitio.Commons.Shared.Utils;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Commons.api.Interfaces
{
    public interface ISplitioHttpClient : IDisposable
    {
        Task<HTTPResult> GetAsync(string url, bool cacheControlHeadersEnabled = false);
        Task<HttpResponseMessage> GetAsync(string url, HttpCompletionOption completionOption, CancellationToken cancellationToken);
        Task<HTTPResult> PostAsync(string url, string data);
    }
}
