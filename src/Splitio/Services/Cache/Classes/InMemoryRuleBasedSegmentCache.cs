using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Classes
{
    public class InMemoryRuleBasedSegmentCache : IRuleBasedSegmentCache
    {
        private readonly object _lock = new object();

        private readonly ConcurrentDictionary<string, RuleBasedSegment> _cache;
        private long _changeNumber;

        public InMemoryRuleBasedSegmentCache(ConcurrentDictionary<string, RuleBasedSegment> cache,
            long changeNumber = -1)
        {
            _cache = cache;
            _changeNumber = changeNumber;
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
            lock (_lock)
            {
                foreach (var rbSegment in toAdd)
                {
                    _cache.AddOrUpdate(rbSegment.Name, rbSegment, (key, oldValue) => rbSegment);
                }

                foreach (var name in toRemove)
                {
                    _cache.TryRemove(name, out var _);
                }

                SetChangeNumber(till);
            }
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
