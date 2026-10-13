using Splitio.Commons.Dto;
using Splitio.Commons.Tasks;
using System;
using System.Threading.Tasks;

namespace Splitio.Services.Tasks
{
    public interface ITasksManager
    {
        ISplitTask NewOnTimeTaskAndStart(Commons.Dto.SplitTaskName taskName, Func<Task> function);
        ISplitTask NewOnTimeTaskAndStart(Commons.Dto.SplitTaskName taskName, Action action);
        ISplitTask NewOnTimeTask(Commons.Dto.SplitTaskName taskName);
        ISplitTask NewScheduledTask(Commons.Dto.SplitTaskName taskName, int intervalMs);
        ISplitTask NewPeriodicTask(Commons.Dto.SplitTaskName taskName, int intervalMs);
        Task DestroyAsync();
    }
}
