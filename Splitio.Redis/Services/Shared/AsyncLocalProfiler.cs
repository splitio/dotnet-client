#if NET_LATEST
using StackExchange.Redis.Profiling;
using System.Threading;

namespace Splitio.Redis.Services.Shared
{
    public class AsyncLocalProfiler
    {
        private readonly AsyncLocal<ProfilingSession> perThreadSession = new AsyncLocal<ProfilingSession>();

        public ProfilingSession GetSession()
        {
            var val = perThreadSession.Value;
            if (val == null)
            {
                perThreadSession.Value = val = new ProfilingSession();
            }
            return val;
        }
    }
}
#endif
