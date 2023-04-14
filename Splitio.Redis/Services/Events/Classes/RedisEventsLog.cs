using Splitio.Domain;
using Splitio.Services.Events.Interfaces;

namespace Splitio.Redis.Services.Events.Classes
{
    public class RedisEvenstLog : IEventsLog
    {
        private readonly IEventCache _eventsCache;

        public RedisEvenstLog(IEventCache eventsCache)
        {
            _eventsCache = eventsCache;
        }

        public void Log(WrappedEvent wrappedEvent)
        {
            _eventsCache.Add(wrappedEvent);
        }

        public void Start()
        {
            throw new System.NotImplementedException();
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }
    }
}