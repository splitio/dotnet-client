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
        { await DoNothing(); }

        public void Start() { }

        public void Stop() { }

        public Task<bool> DoNothing()
        { return Task.FromResult(true); }
    }
}
