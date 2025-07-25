﻿using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Shared.Classes;
using System.Collections.Generic;
using System.IO;

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
                var segmentChangesResult = JsonConvertWrapper.DeserializeObject<SegmentChange>(json);
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
