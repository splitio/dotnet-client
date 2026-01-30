using System.Threading.Tasks;

namespace Splitio.Services.Cache.Interfaces
{
    public interface IStatusManager
    {
        bool IsReady();
        bool WaitUntilReady(int milliseconds);
        Task SetReadyAsync();
        void SetDestroy();
        bool IsDestroyed();
    }
}
