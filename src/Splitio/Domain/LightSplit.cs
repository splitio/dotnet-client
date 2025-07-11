﻿using System.Collections.Generic;

namespace Splitio.Domain
{
    public class SplitView
    {
        public string name { get; set; }
        public string trafficType { get; set; }
        public bool killed { get; set; }
        public List<string> treatments { get; set; }
        public long changeNumber { get; set; }
        public Dictionary<string, string> configs { get; set; }
        public string defaultTreatment { get; set; }
        public List<string> sets { get; set; }
        public bool impressionsDisabled { set; get; }
        public List<PrerequisitesDto> prerequisites { get; set; }
    }
}
