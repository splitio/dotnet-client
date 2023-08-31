using System;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    public interface ISplitTask
    {
        void Start();
        Task StopAsync();
        void SetAction(Action action);
        void SetFunction(Func<Task> function);
        void SetInterval(int interval);
        void OnStop(Func<Task> function);
        void OnStop(Action action);
        bool IsRunning();
    }
}
