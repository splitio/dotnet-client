namespace Splitio.Services.Client.Interfaces
{
    public interface ISplitClient : ISplitClientSync, ISplitClientAsync
    {
        ISplitManager GetSplitManager();
        bool IsDestroyed();
        void BlockUntilReady(int blockMilisecondsUntilReady);
    }
}
