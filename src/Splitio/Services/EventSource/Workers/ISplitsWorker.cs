namespace Splitio.Services.EventSource.Workers
{
    public interface ISplitsWorker : IWorker
    {
        void AddToQueue(SplitChangeNotification scn);
        void Kill(SplitKillNotification skn);
    }
}
