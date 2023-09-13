using System.Threading.Tasks;

namespace Splitio.Services.Shared.Interfaces
{
    public interface IPeriodicTask
    {
        void Start();
        Task StopAsync();
    }
}
