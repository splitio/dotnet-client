using Splitio.Commons.Domain;
using Splitio.Commons.Dto;
using System.Threading.Tasks;

namespace Splitio.Commons.Tasks
{
    public interface IInternalEventsTask
    {
        Task AddToQueue(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata);
        void Start();
        void Stop();
    }
}
