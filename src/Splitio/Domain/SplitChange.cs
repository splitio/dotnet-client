using System.Collections.Generic;

namespace Splitio.Domain
{
    public class SplitChangesResult
    {
        public long Since { get; set; }
        public long Till { get; set; }
        public List<Split> Splits { get; set; }
    }
}
