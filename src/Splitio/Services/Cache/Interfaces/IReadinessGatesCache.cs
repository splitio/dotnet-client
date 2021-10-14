namespace Splitio.Services.Cache.Interfaces
{
    public interface IReadinessGatesCache
    {
        bool IsReady();
        bool WaitUntilReady(int milliseconds);
        void SetReady();
    }
}
