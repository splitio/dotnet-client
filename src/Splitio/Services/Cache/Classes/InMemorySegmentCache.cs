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

        private ConcurrentDictionary<string, Segment> _segments;

        public InMemorySegmentCache(ConcurrentDictionary<string, Segment> segments)
        {
            _segments = segments;
        }

        public Task AddToSegmentAsync(string segmentName, List<string> segmentKeys)
        {
            _segments.TryGetValue(segmentName, out Segment segment);

            if (segment == null)
            {
                segment = new Segment(segmentName);
                _segments.TryAdd(segmentName, segment);
            }

            segment.AddKeys(segmentKeys);

            return Task.FromResult(0);
        }

        public Task RemoveFromSegmentAsync(string segmentName, List<string> segmentKeys)
        {
            if (_segments.TryGetValue(segmentName, out Segment segment))
            {
                segment.RemoveKeys(segmentKeys);
            }

            return Task.FromResult(0);
        }

        public Task<bool> IsInSegmentAsync(string segmentName, string key)
        {
            if (_segments.TryGetValue(segmentName, out Segment segment))
            {
                return Task.FromResult(segment.Contains(key));
            }

            return Task.FromResult(false);
        }

        public Task SetChangeNumberAsync(string segmentName, long changeNumber)
        {
            if (_segments.TryGetValue(segmentName, out Segment segment))
            {
                if (changeNumber < segment.changeNumber)
                {
                    _log.Error("ChangeNumber for segment cache is less than previous");
                }
                segment.changeNumber = changeNumber;              
            }

            return Task.FromResult(0);
        }

        public Task<long> GetChangeNumberAsync(string segmentName)
        {
            if (_segments.TryGetValue(segmentName, out Segment segment))
            {
                return Task.FromResult(segment.changeNumber);
            }

            return Task.FromResult(-1L);
        }

        public void Clear()
        {
            _segments.Clear();
        }

        public Task<List<string>> GetSegmentNamesAsync()
        {
            var names = _segments
                .Keys
                .ToList();

            return Task.FromResult(names);
        }

        public Task<List<string>> GetSegmentKeysAsync(string segmentName)
        {
            if (_segments.TryGetValue(segmentName, out Segment segment))
            {
                return Task.FromResult(segment.GetKeys());
            }

            return Task.FromResult(new List<string>());
        }

        public async Task<int> SegmentsCountAsync()
        {
            var names = await GetSegmentNamesAsync();

            return names.Count;
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
    }
}
