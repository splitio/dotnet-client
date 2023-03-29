using System.Net;

namespace Splitio.CommonLibraries
{
    public class HTTPResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Content { get; set; }
        public bool IsSuccessStatusCode { get; set; }
    }
}
