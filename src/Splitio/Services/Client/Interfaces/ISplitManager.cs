using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Client.Interfaces
{
    public interface ISplitManager
    {
        List<SplitView> Splits();
        List<string> SplitNames();
        SplitView Split(string featureName);
        void BlockUntilReady(int blockMilisecondsUntilReady);

        Task<List<SplitView>> SplitsAsync();
        Task<List<string>> SplitNamesAsync();
        Task<SplitView> SplitAsync(string featureName);
    }
}
