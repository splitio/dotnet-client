using System.Threading.Tasks;

namespace Splitio.Commons.Tasks
{
    public interface IPeriodicTask
    {
        void Start();
        Task StopAsync();
    }
}
