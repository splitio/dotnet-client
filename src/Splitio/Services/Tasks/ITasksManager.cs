using System;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    public interface ITasksManager
    {
        ISplitTask NewOnTimeTaskAndStart(Enums.Task taskName, Action action);
        ISplitTask NewOnTimeTask(Enums.Task taskName);
        ISplitTask NewScheduledTask(Enums.Task taskName, int intervalMs);
        ISplitTask NewPeriodicTask(Enums.Task taskName, int intervalMs);
        Task DestroyAsync();
    }
}
