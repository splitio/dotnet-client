using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio.Services.Cache.Classes
{
    public class InMemorySegmentCache : ISegmentCache
    {
        private static readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(InMemorySegmentCache));

        private readonly ConcurrentDictionary<string, Segment> _segments;

        public InMemorySegmentCache(ConcurrentDictionary<string, Segment> segments)
        {
            _segments = segments;
        }

        #region Methods Sync
        public void AddToSegment(string segmentName, List<string> segmentKeys)
        {
            _segments.TryGetValue(segmentName, out Segment segment);

            if (segment == null)
            {
                segment = new Segment(segmentName);
                _segments.TryAdd(segmentName, segment);
            }

            segment.AddKeys(segmentKeys);
        }

        public void RemoveFromSegment(string segmentName, List<string> segmentKeys)
        {
            if (_segments.TryGetValue(segmentName, out Segment segment))
            {
                segment.RemoveKeys(segmentKeys);
            }
        }

        public bool IsInSegment(string segmentName, string key)
        {
            if (_segments.TryGetValue(segmentName, out Segment segment))
            {
                return segment.Contains(key);
            }

            return false;
        }

        public void SetChangeNumber(string segmentName, long changeNumber)
        {
            if (_segments.TryGetValue(segmentName, out Segment segment))
            {
                if (changeNumber < segment.changeNumber)
                {
                    _log.Error("ChangeNumber for segment cache is less than previous");
                }
                segment.changeNumber = changeNumber;              
            }
        }

        public long GetChangeNumber(string segmentName)
        {
            if (_segments.TryGetValue(segmentName, out Segment segment))
            {
                return segment.changeNumber;
            }

            return -1;
        }

        public void Clear()
        {
            _segments.Clear();
        }

        public List<string> GetSegmentNames()
        {
            return _segments
                .Keys
                .ToList();
        }

        public List<string> GetSegmentKeys(string segmentName)
        {
            if (_segments.TryGetValue(segmentName, out Segment segment))
            {
                return segment.GetKeys();
            }

            return new List<string>();
        }

        public int SegmentsCount()
        {
            return GetSegmentNames().Count;
        }

        public int SegmentKeysCount()
        {
            var keys = 0;

            var names = _segments.Keys;

            foreach (var segmentName in names)
            {
                if (_segments.TryGetValue(segmentName, out Segment segment))
                {
                    keys += segment.GetKeys().Count;
                }
            }

            return keys;
        }
        #endregion

        #region Methods Async
        public Task AddToSegmentAsync(string segmentName, List<string> segmentKeys)
        {
            AddToSegment(segmentName, segmentKeys);

            return Task.FromResult(0);
        }

        public Task RemoveFromSegmentAsync(string segmentName, List<string> segmentKeys)
        {
            RemoveFromSegment(segmentName, segmentKeys);

            return Task.FromResult(0);
        }

        public Task<bool> IsInSegmentAsync(string segmentName, string key)
        {
            return Task.FromResult(IsInSegment(segmentName, key));
        }

        public Task SetChangeNumberAsync(string segmentName, long changeNumber)
        {
            SetChangeNumber(segmentName, changeNumber);

            return Task.FromResult(0);
        }

        public Task<long> GetChangeNumberAsync(string segmentName)
        {
            return Task.FromResult(GetChangeNumber(segmentName));
        }

        public Task ClearAsync()
        {
            Clear();

            return Task.FromResult(0);
        }

        public Task<List<string>> GetSegmentNamesAsync()
        {
            return Task.FromResult(GetSegmentNames());
        }

        public Task<List<string>> GetSegmentKeysAsync(string segmentName)
        {
            return Task.FromResult(GetSegmentKeys(segmentName));
        }

        public Task<int> SegmentsCountAsync()
        {
            return Task.FromResult(SegmentsCount());
        }

        public Task<int> SegmentKeysCountAsync()
        {
            return Task.FromResult(SegmentKeysCount());
        }
        #endregion
    }
}
