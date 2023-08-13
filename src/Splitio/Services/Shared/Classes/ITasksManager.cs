using Splitio.Services.Shared.Interfaces;
using System.Timers;

namespace Splitio.Services.Shared.Classes
{
    public interface ITasksManager
    {
        //void Start(Action action, string description);
        //void Start(Action action, CancellationTokenSource cancellationToken, string description);
        //void StartPeriodic(Action action, int intervalInMilliseconds, CancellationTokenSource cancellationToken, string description);

        ISplitTask NewOnTimeTask(Enums.Task taskName);
        ISplitTask NewPeriodicTask(Enums.Task taskName, int intervalMs);
        void Destroy();
    }
}
