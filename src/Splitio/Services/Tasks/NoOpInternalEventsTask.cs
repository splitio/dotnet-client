using Splitio.Domain;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    public class NoOpInternalEventsTask : IInternalEventsTask
    {
        public NoOpInternalEventsTask()
        {
            return;
        }
        public async Task AddToQueue(SdkInternalEvent sdkInternalEvent, EventMetadata eventMetadata)
        { return; }

        public void Start() { return; }

        public void Stop() { return; }
    }
}
