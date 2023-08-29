using Splitio.Services.Cache.Interfaces;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    public interface ITasksManager
    {
        ISplitTask NewOnTimeTaskAndStart(IStatusManager statusManager, Enums.Task taskName, Action action);
        ISplitTask NewOnTimeTask(IStatusManager statusManager, Enums.Task taskName);
        ISplitTask NewScheduledTask(IStatusManager statusManager, Enums.Task taskName, int intervalMs);
        ISplitTask NewPeriodicTask(IStatusManager statusManager, Enums.Task taskName, int intervalMs);
        void Destroy();
    }
}
