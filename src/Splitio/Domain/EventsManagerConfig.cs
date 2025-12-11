using System.Collections.Generic;

namespace Splitio.Domain
{
    public class EventsManagerConfig
    {
        public Dictionary<SdkEvent, HashSet<SdkInternalEvent>> RequireAll { get; private set; }
        public Dictionary<SdkEvent, HashSet<SdkInternalEvent>> RequireAny { get; private set; }
        public Dictionary<SdkEvent, HashSet<SdkEvent>> Prerequisites { get; private set; }
        public Dictionary<SdkEvent, HashSet<SdkInternalEvent>> SuppressedBy { get; private set; }
        public Dictionary<SdkEvent, int> ExecutionLimits { get; private set; }

        private EventsManagerConfig(
            Dictionary<SdkEvent, HashSet<SdkInternalEvent>> requireAll,
            Dictionary<SdkEvent, HashSet<SdkInternalEvent>> requireAny,
            Dictionary<SdkEvent, HashSet<SdkEvent>> prerequisites,
            Dictionary<SdkEvent, HashSet<SdkInternalEvent>> suppressedBy,
            Dictionary<SdkEvent, int> executionLimits)
        { 
            RequireAll = requireAll;
            RequireAny = requireAny;
            Prerequisites = prerequisites;
            SuppressedBy = suppressedBy;
            ExecutionLimits = executionLimits;
        }

        public static EventsManagerConfig BuildEventsManagerConfig()
        {
            Dictionary<SdkEvent, HashSet<SdkInternalEvent>> requireAll = new Dictionary<SdkEvent, HashSet<SdkInternalEvent>>
            {
                {
                    SdkEvent.SdkReady, new HashSet<SdkInternalEvent>
                    {
                        SdkInternalEvent.SdkReady
                    }
                }
            };

            Dictionary<SdkEvent, HashSet<SdkEvent>> prerequisites = new Dictionary<SdkEvent, HashSet<SdkEvent>>
            {
                {
                    SdkEvent.SdkUpdate, new HashSet<SdkEvent>
                    {
                        SdkEvent.SdkReady
                    }
                }
            };

            Dictionary<SdkEvent, HashSet<SdkInternalEvent>> requireAny = new Dictionary<SdkEvent, HashSet<SdkInternalEvent>>
            {
                { SdkEvent.SdkUpdate, new HashSet<SdkInternalEvent> 
                    {
                        SdkInternalEvent.RuleBasedSegmentsUpdated,
                        SdkInternalEvent.FlagsUpdated,
                        SdkInternalEvent.FlagKilledNotification,
                        SdkInternalEvent.SegmentsUpdated
                    }
                },
                { SdkEvent.SdkReadyTimeout, new HashSet<SdkInternalEvent>
                    {
                        SdkInternalEvent.SdkTimedOut
                    }
                }
            };

            Dictionary<SdkEvent, HashSet<SdkInternalEvent>> suppressedBy = new Dictionary<SdkEvent, HashSet<SdkInternalEvent>>
            {
                { SdkEvent.SdkReadyTimeout, new HashSet<SdkInternalEvent>
                    { SdkInternalEvent.SdkReady }

                }
            };

            Dictionary<SdkEvent, int> executionLimits = new Dictionary<SdkEvent, int>
            {
                { SdkEvent.SdkReadyTimeout, 1 },
                { SdkEvent.SdkReady, 1 },
                { SdkEvent.SdkUpdate, -1 }
            };

            return new EventsManagerConfig(requireAll, requireAny, prerequisites, suppressedBy, executionLimits);
        }
    }
}
