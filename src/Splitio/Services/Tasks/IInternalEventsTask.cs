using Splitio.Commons.Domain;
using Splitio.Commons.Dto;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    public interface IInternalEventsTask
    {
        Task AddToQueue(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata);
        void Start();
        void Stop();
    }
}
