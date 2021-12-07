using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.Events.Interfaces
{
    public interface IEventSdkApiClient
    {
        void SendBulkEventsTask(List<Event> events);
    }
}
