namespace Splitio.Services.EventSource.Workers
{
    public interface ISplitsWorker : IWorker
    {
        void AddToQueue(long changeNumber);
    }
}
