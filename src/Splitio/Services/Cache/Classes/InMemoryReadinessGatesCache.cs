using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using System.Collections.Generic;
using System.Threading;

namespace Splitio.Services.Client.Classes
{
    public class InMemoryReadinessGatesCache : IStatusManager
    {
        private readonly CountdownEvent _sdkReady = new CountdownEvent(1);
        private readonly CountdownEvent _sdkDestroyed = new CountdownEvent(1);
        private readonly EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> _eventsManager;

        public InMemoryReadinessGatesCache(EventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManager) 
        {
            _eventsManager = eventsManager;
        }

        public bool IsReady()
        {
            return _sdkReady.IsSet;
        }

        public bool WaitUntilReady(int milliseconds)
        {
            return _sdkReady.Wait(milliseconds);
        }

        public void SetReady()
        {
            _sdkReady.Signal();
            _eventsManager.NotifyInternalEvent(SdkInternalEvent.SdkReady,
                new EventMetadata(new Dictionary<string, object>()));
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
