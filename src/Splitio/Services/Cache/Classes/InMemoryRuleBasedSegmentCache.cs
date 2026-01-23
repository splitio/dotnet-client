using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Common;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Classes
{
    public class InMemoryRuleBasedSegmentCache : IRuleBasedSegmentCache
    {
        private readonly ConcurrentDictionary<string, RuleBasedSegment> _cache;
        private long _changeNumber;
        private readonly IEventsManager<SdkEvent, SdkInternalEvent, EventMetadata> _eventsManager;

        public InMemoryRuleBasedSegmentCache(ConcurrentDictionary<string, RuleBasedSegment> cache,
            IEventsManager<SdkEvent, SdkInternalEvent, EventMetadata> eventsManger,
            long changeNumber = -1)
        {
            _cache = cache;
            _changeNumber = changeNumber;
            _eventsManager = eventsManger;
        }

        #region Sync Methods
        // Consumer
        public bool Contains(List<string> names)
        {
            foreach (string name in names)
            {
                if (!_cache.ContainsKey(name)) return false;
            }

            return true;
        }

        public RuleBasedSegment Get(string name)
        {
            _cache.TryGetValue(name, out RuleBasedSegment segment);

            return segment;
        }

        public long GetChangeNumber()
        {
            return _changeNumber;
        }

        // Producer
        public void Update(List<RuleBasedSegment> toAdd, List<string> toRemove, long till)
        {
            List<string> toNotify = new List<string>();
            foreach (var rbSegment in toAdd)
            {
                _cache.AddOrUpdate(rbSegment.Name, rbSegment, (key, oldValue) => rbSegment);
                toNotify.Add(rbSegment.Name);
            }

            foreach (var name in toRemove)
            {
                _cache.TryRemove(name, out var _);
                toNotify.Add(name);
            }

            SetChangeNumber(till);
            _eventsManager.NotifyInternalEvent(SdkInternalEvent.RuleBasedSegmentsUpdated,
                new EventMetadata(SdkEventType.SegmentsUpdate, toNotify));
        }

        public void SetChangeNumber(long changeNumber)
        {
            _changeNumber = changeNumber;
        }

        public void Clear()
        {
            _cache.Clear();
        }
        #endregion

        #region Async Methods
        public Task<RuleBasedSegment> GetAsync(string name)
        {
            return Task.FromResult(Get(name));
        }
        #endregion
    }
}
