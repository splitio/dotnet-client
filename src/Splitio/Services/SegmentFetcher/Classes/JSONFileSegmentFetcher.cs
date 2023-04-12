using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class JSONFileSegmentFetcher : SegmentFetcher
    {
        private readonly List<string> _added;

        public JSONFileSegmentFetcher(string filePath, 
            ISegmentCache segmentsCache) : base(segmentsCache)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var json = File.ReadAllText(filePath);
                var segmentChangesResult = JsonConvert.DeserializeObject<SegmentChange>(json);
                _added = segmentChangesResult.added;
            }
        }

        public override async Task InitializeSegmentAsync(string name)
        {
            if (_added != null)
            {
                await _segmentCache.AddToSegmentAsync(name, _added);
            }
        }
    }
}
