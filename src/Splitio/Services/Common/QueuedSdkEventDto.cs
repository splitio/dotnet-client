using Splitio.Domain;
using System;

namespace Splitio.Services.EventSource.Workers
{
    public class QueuedSdkEventDto
    {
        public SdkEvent SdkEvent { get; set; }
        public EventMetadata EventMetadata { get; set; }
    }
}
