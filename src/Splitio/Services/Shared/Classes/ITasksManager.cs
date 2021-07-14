using System;
using System.Threading;

namespace Splitio.Services.Shared.Classes
{
    public interface ITasksManager
    {
        void Start(Action action, CancellationTokenSource cancellationToken, string description);
        void StartPeriodic(Action action, int intervalInMilliseconds, CancellationTokenSource cancellationToken, string description);
        void CancelAll();
    }
}
