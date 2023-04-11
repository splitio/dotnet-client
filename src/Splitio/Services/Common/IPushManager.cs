using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public interface IPushManager
    {
        Task<bool> StartSseAsync();
        void StopSse();
    }
}
