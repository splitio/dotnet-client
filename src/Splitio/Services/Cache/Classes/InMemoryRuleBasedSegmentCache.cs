using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Classes
{
    public class InMemoryRuleBasedSegmentCache : IRuleBasedSegmentCache
    {
        private readonly ConcurrentDictionary<string, RuleBasedSegment> _cache;
        private long _changeNumber;
        private readonly IInternalEventsTask _internalEventsTask;
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(InMemoryRuleBasedSegmentCache));

        public InMemoryRuleBasedSegmentCache(ConcurrentDictionary<string, RuleBasedSegment> cache,
            IInternalEventsTask internalEventsTask,
            long changeNumber = -1)
        {
            _cache = cache;
            _changeNumber = changeNumber;
            _internalEventsTask = internalEventsTask;
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
            foreach (var rbSegment in toAdd)
            {
                _cache.AddOrUpdate(rbSegment.Name, rbSegment, (key, oldValue) => rbSegment);
            }

            foreach (var name in toRemove)
            {
                _cache.TryRemove(name, out var _);
            }

            SetChangeNumber(till);
            _internalEventsTask.AddToQueue(SdkInternalEvent.RuleBasedSegmentsUpdated,
                new EventMetadata(SdkEventType.SegmentsUpdate, new List<string>())).ContinueWith(OnAddToQueueFailed, TaskContinuationOptions.OnlyOnFaulted);
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

        public void OnAddToQueueFailed(Task task)
        {
            _log.Error($"Failed to add internal event to queue: {task.Exception.Message}");
        }
    }
}
