using System.Threading.Tasks;

namespace Splitio.Services.Localhost
{
    public interface ISplitFileWatcher
    {
        void Start();
        Task StopAsync();
    }
}
