using System.Threading.Tasks;

namespace Splitio.Services.EventSource.Workers
{
    public interface ISplitsWorker : IWorker
    {
        void AddToQueue(long changeNumber);
        Task KillSplitAsync(long changeNumber, string splitName, string defaultTreatment);   
    }
}
