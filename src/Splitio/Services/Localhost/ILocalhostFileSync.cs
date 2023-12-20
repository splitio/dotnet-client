using System;
using System.Threading.Tasks;

namespace Splitio.Services.Localhost
{
    public interface ILocalhostFileSync
    {
        void SetOnFileChangedAction(Action onFileChanged);
        void Start(string filePath);
        Task StopAsync();
    }
}
