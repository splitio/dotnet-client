using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public interface ISyncManager
    {
        void Start();
        void Shutdown();
    }
}
