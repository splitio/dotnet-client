using Splitio.Domain;
using Splitio.Services.Cache.Interfaces;
using Splitio.Services.Logger;
using Splitio.Services.Shared.Classes;
using Splitio.Services.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Client.Classes
{
    public class InMemoryReadinessGatesCache : IStatusManager
    {
        private readonly CountdownEvent _sdkReady = new CountdownEvent(1);
        private readonly CountdownEvent _sdkDestroyed = new CountdownEvent(1);
        private readonly IInternalEventsTask _internalEventsTask;
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(InMemoryReadinessGatesCache));

        public InMemoryReadinessGatesCache(IInternalEventsTask internalEventsTask) 
        {
            _internalEventsTask = internalEventsTask;
        }

        public bool IsReady()
        {
            return _sdkReady.IsSet;
        }

        public bool WaitUntilReady(int milliseconds)
        {
            return _sdkReady.Wait(milliseconds);
        }

        public void SetReady()
        {
            _sdkReady.Signal();
            _internalEventsTask.AddToQueue(SdkInternalEvent.SdkReady, null).ContinueWith(OnAddToQueueFailed, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void SetDestroy()
        {
            _sdkDestroyed.Signal();
        }

        public bool IsDestroyed()
        {
            return _sdkDestroyed.IsSet;
        }

        public void OnAddToQueueFailed(Task task)
        {
            _log.Error($"Failed to add internal event to queue: {task.Exception.Message}");
        }
    }
}
