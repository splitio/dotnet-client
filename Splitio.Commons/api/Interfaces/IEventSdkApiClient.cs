using Splitio.Commons.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Commons.api.Interfaces
{
    public interface IEventSdkApiClient
    {
        Task SendBulkEventsAsync(List<Event> events);
    }
}
