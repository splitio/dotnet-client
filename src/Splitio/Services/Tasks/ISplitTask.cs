using System;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    public interface ISplitTask
    {
        void Start();
        void Stop();
        void SetAction(Action action);
        void SetFunction(Func<Task> function);
        bool IsRunning();
        void SetInterval(int interval);
    }
}
