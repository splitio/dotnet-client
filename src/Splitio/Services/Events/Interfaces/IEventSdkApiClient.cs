using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Events.Interfaces
{
    public interface IEventSdkApiClient
    {
        Task SendBulkEventsAsync(List<Event> events);
    }
}
