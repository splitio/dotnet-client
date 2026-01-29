using System.Collections.Generic;
using System.Linq;

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
                }
            };

            SuppressedBy = new Dictionary<SdkEvent, HashSet<SdkEvent>>();

            ExecutionLimits = new Dictionary<SdkEvent, int>
            {
                { SdkEvent.SdkReady, 1 },
                { SdkEvent.SdkUpdate, -1 }
            };

            HashSet<SdkEvent> sortedEvents = new HashSet<SdkEvent>();
            foreach (SdkEvent sdkEvent in new List<SdkEvent> { SdkEvent.SdkReady, SdkEvent.SdkUpdate }) 
            {
                sortedEvents = DFSRecursive(sdkEvent, sortedEvents);
            }

            EvaluationOrder = sortedEvents;
        }

        private HashSet<SdkEvent> DFSRecursive(SdkEvent sdkEvent, HashSet<SdkEvent> added)
        {
            if (added.Contains(sdkEvent)) return added;

            foreach (SdkEvent dependentEvent in GetDependencies(sdkEvent))
            {
                added = DFSRecursive(dependentEvent, added);
            }

            added.Add(sdkEvent);

            return added;
        }

        private HashSet<SdkEvent> GetDependencies(SdkEvent sdkEvent)
        {
            HashSet<SdkEvent> dependencies = new HashSet<SdkEvent>();
            foreach (KeyValuePair<SdkEvent, HashSet<SdkEvent>> prerequisitesEvent in Prerequisites.Where(x => x.Key.Equals(sdkEvent)))
            {
                foreach (var prereqEvent in prerequisitesEvent.Value)
                {
                    dependencies.Add(prereqEvent);
                }
            }

            foreach (KeyValuePair<SdkEvent, HashSet<SdkEvent>> suppressedEvent in SuppressedBy.Where(x => x.Value.Contains(sdkEvent)))
            {
                dependencies.Add(suppressedEvent.Key);
            }

            return dependencies;
        }
    }
}
