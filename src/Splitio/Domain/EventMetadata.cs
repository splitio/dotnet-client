using System.Collections.Generic;

namespace Splitio.Domain
{
    public class EventMetadata
    {
        private readonly List<string> _names;
        private readonly SdkEventType _type;

        public EventMetadata(SdkEventType type, List<string> names) 
        {
            _type = type;
            _names = names;
        }

        public List<string> GetNames() { return _names; }

        public SdkEventType GetEventType() { return _type; }
    }
}
