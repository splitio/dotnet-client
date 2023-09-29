using Splitio.Domain;
using Splitio.Services.Events.Interfaces;
using Splitio.Services.Shared.Interfaces;
using Splitio.Services.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Redis.Services.Events.Classes
{
    public class RedisEvenstLog : IEventsLog
    {
        private readonly ISimpleCache<WrappedEvent> _eventsCache;
        private readonly ITasksManager _tasksManager;

        public RedisEvenstLog(ISimpleCache<WrappedEvent> eventsCache,
            ITasksManager tasksManager)
        {
            _eventsCache = eventsCache;
            _tasksManager = tasksManager;
        }

        public void Log(WrappedEvent wrappedEvent)
        {
            _eventsCache.AddItems(new List<WrappedEvent> { wrappedEvent });
        }

        public async Task LogAsync(WrappedEvent wrappedEvent)
        {
            await _eventsCache.AddItemsAsync(new List<WrappedEvent> { wrappedEvent });
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