using System.Threading.Tasks;

namespace Splitio.Commons.Cache.Interfaces
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
