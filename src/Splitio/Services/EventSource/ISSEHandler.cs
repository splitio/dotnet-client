using System.Threading.Tasks;

namespace Splitio.Services.EventSource
{
    public interface ISSEHandler
    {
        bool Start(string token, string channels);
        void Stop();
        void StartWorkers();
        void StopWorkers();
    }
}
