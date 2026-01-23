using System.Collections.Generic;
using Splitio.Services.InputValidation.Classes;
using System.Linq;

namespace Splitio.Domain
{
    public class EventMetadata
    {
        private List<string> _names;
        private SdkEventType _type;

        public EventMetadata(SdkEventType type, List<string> names) 
        {
            _type = type;
            _names = names;
        }

        public List<string> GetNames() { return _names; }

        public SdkEventType GetEventType() { return _type; }
    }
}
