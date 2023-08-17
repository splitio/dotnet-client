using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public interface ISSEHandler
    {
        bool Start(string token, string channels);
        Task StopAsync();
        void StartWorkers();
        Task StopWorkersAsync();
    }
}
