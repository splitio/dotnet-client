using System.Timers;

namespace Splitio.Services.Shared.Interfaces
{
    public interface ISplitTask : IPeriodicTask
    {
        void SetInterval(double interval);
        void SetEventHandler(ElapsedEventHandler elapsed);
        bool IsRunning();
        void Kill();
    }
}
