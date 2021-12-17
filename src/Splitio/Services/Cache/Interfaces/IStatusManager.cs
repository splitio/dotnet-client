namespace Splitio.Services.Cache.Interfaces
{
    public interface IStatusManager
    {
        bool IsReady();
        bool WaitUntilReady(int milliseconds);
        void SetReady();
        void SetDestroy();
        bool IsDestroyed();
    }
}
