using System;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Classes
{
    public interface ITasksManager
    {
        void Start(Func<Task> function, CancellationTokenSource cancellationToken, string description);
        void Start(Action action, string description);
        void Start(Action action, CancellationTokenSource cancellationToken, string description);
        void StartPeriodic(Action action, int intervalInMilliseconds, CancellationTokenSource cancellationToken, string description);
    }
}
