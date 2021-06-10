using System.Threading.Tasks;

namespace Splitio.CommonLibraries
{
    public interface ISdkApiClient
    {
        Task<HTTPResult> ExecuteGet(string requestUri, bool cacheControlHeadersEnabled = false);

        Task<HTTPResult> ExecutePost(string requestUri, string data);
    }
}
