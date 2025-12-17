using System.Collections.Generic;

namespace Splitio.Domain
{
    public class EventsManagerConfig : EventManagerConfigData<SdkEvent, SdkInternalEvent>
    {
        public EventsManagerConfig() 
        {
            RequireAll = new Dictionary<SdkEvent, HashSet<SdkInternalEvent>>
            {
                {
                    SdkEvent.SdkReady, new HashSet<SdkInternalEvent>
                    {
                        SdkInternalEvent.SdkReady
                    }
                }
            };

            Prerequisites = new Dictionary<SdkEvent, HashSet<SdkEvent>>
            {
                {
                    SdkEvent.SdkUpdate, new HashSet<SdkEvent>
                    {
                        SdkEvent.SdkReady
                    }
                }
            };

            RequireAny = new Dictionary<SdkEvent, HashSet<SdkInternalEvent>>
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

            SuppressedBy = new Dictionary<SdkEvent, HashSet<SdkEvent>>
            {
                { SdkEvent.SdkReadyTimeout, new HashSet<SdkEvent>
                    { SdkEvent.SdkReady }

                }
            };

            ExecutionLimits = new Dictionary<SdkEvent, int>
            {
                { SdkEvent.SdkReadyTimeout, -1 },
                { SdkEvent.SdkReady, 1 },
                { SdkEvent.SdkUpdate, -1 }
            };
        }
    }
}
