using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public interface ISSEHandler
    {
        void Start(string token, string channels);
        Task StopAsync();
        void StartWorkers();
        void StopWorkers();
    }
}
