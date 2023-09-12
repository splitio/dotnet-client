using System.Threading.Tasks;

namespace Splitio.Services.EventSource.Workers
{
    public interface IWorker
    {
        void Start();
        Task StopAsync();
    }
}
