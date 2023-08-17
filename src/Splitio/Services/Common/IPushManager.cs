using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public interface IPushManager
    {
        Task Start();
        Task StopAsync();
        Task ScheduleConnectionResetAsync();
    }
}
