using System;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    public interface ISplitTask
    {
        void Start();
        Task StopAsync();
        void SetAction(Action action);
        bool IsRunning();
        void SetInterval(int interval);
    }
}
