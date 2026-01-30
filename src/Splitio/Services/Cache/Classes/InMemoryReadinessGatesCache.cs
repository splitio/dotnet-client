using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Client.Classes
{
    public class InMemoryReadinessGatesCache : IStatusManager
    {
        private readonly CountdownEvent _sdkReady = new CountdownEvent(1);
        private readonly CountdownEvent _sdkDestroyed = new CountdownEvent(1);
        private readonly IInternalEventsTask _internalEventsTask;

        public InMemoryReadinessGatesCache(IInternalEventsTask internalEventsTask)
        {
            _internalEventsTask = internalEventsTask;
        }

        public bool IsReady()
        {
            return _sdkReady.IsSet;
        }

        public bool WaitUntilReady(int milliseconds)
        {
            return _sdkReady.Wait(milliseconds);
        }

        public async Task SetReadyAsync()
        {
            _sdkReady.Signal();
            await _internalEventsTask.AddToQueue(SdkInternalEvent.SdkReady, null);
        }

        public void SetDestroy()
        {
            _sdkDestroyed.Signal();
        }

        public bool IsDestroyed()
        {
            return _sdkDestroyed.IsSet;
        }
    }
}
