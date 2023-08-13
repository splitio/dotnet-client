using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public interface IPushManager
    {
        Task Start();
        void Stop();
        void ScheduleConnectionReset();
    }
}
