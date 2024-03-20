using System.Threading.Tasks;

namespace Splitio.Services.EventSource.Workers
{
    public interface ISegmentsWorker : IWorker
    {
        Task AddToQueue(long changeNumber, string segmentName);
    }
}
