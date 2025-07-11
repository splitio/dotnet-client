using Newtonsoft.Json;
using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using System.Collections.Generic;
using System.IO;
using Splitio.Common;

namespace Splitio.Services.SegmentFetcher.Classes
{
    public class JSONFileSegmentFetcher : SegmentFetcher
    {
        readonly List<string> added;

        public JSONFileSegmentFetcher(string filePath, 
            ISegmentCache segmentsCache) : base(segmentsCache)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var json = File.ReadAllText(filePath);
                var segmentChangesResult = JsonConvert.DeserializeObject<SegmentChange>(json, SerializerSettings.DefaultSerializerSettings);
                added = segmentChangesResult.added;
            }
        }

        public override void InitializeSegment(string name)
        {
            if (added != null)
            {
                _segmentCache.AddToSegment(name, added);
            }
        }
    }
}
