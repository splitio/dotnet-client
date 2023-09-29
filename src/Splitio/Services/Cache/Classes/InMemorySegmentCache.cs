using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public int SegmentsCount()
        {
            return _segments.Count;
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

        public Task<bool> IsInSegmentAsync(string segmentName, string key)
        {
            return Task.FromResult(IsInSegment(segmentName, key));
        }
        #endregion
    }
}
