using System;
using System.Collections.Generic;

namespace Splitio.Domain
{
    public class EventManagerConfigData<E, I>
    {
        public Dictionary<E, HashSet<I>> RequireAll { get; set; }
        public Dictionary<E, HashSet<I>> RequireAny { get; set; }
        public Dictionary<E, HashSet<E>> Prerequisites { get; set; }
        public Dictionary<E, HashSet<E>> SuppressedBy { get; set; }
        public Dictionary<E, int> ExecutionLimits { get; set; }
    }
}
