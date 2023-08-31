using Splitio.Domain;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Events.Classes
{
    public class RedisEvenstLog : IEventsLog
    {
        private readonly ISimpleCache<WrappedEvent> _eventsCache;

        public RedisEvenstLog(ISimpleCache<WrappedEvent> eventsCache)
        {
            _eventsCache = eventsCache;
        }

        public void Log(WrappedEvent wrappedEvent)
        {
            _eventsCache.AddItems(new List<WrappedEvent> { wrappedEvent });
        }

        public void Start()
        {
            throw new System.NotImplementedException();
        }

        public Task StopAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}