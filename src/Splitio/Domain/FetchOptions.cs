using System.Threading;

namespace Splitio.Domain
{
    public class FetchOptions
    {
        public long? Till { get; set; }
        public bool CacheControlHeaders { get; set; }
        public CancellationToken Token { get; set; }
    }
}
