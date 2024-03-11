using System.Collections.Generic;

namespace Splitio.Domain
{
    public abstract class SplitBase
    {
        public string Name { get; set; }
        public int Seed { get; set; }
        public bool Killed { get; set; }
        public string DefaultTreatment { get; set; }
        public long ChangeNumber { get; set; }
        public string TrafficTypeName { get; set; }
        public int TrafficAllocation { get; set; }
        public Dictionary<string, string> Configurations { get; set; }
        public HashSet<string> Sets { get; set; }
    }
}
