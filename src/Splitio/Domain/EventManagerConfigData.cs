using System.Collections.Generic;

namespace Splitio.Domain
{
    public class EventManagerConfigData<E, I>
    {
        public Dictionary<E, HashSet<I>> RequireAll { get; protected set ; }
        public Dictionary<E, HashSet<I>> RequireAny { get; protected set; }
        public Dictionary<E, HashSet<E>> Prerequisites { get; protected set; }
        public Dictionary<E, HashSet<E>> SuppressedBy { get; protected set; }
        public Dictionary<E, int> ExecutionLimits { get; protected set; }
    }
}
