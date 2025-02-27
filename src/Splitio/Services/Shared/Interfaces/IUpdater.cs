using System.Collections.Generic;

namespace Splitio.Services.Shared.Interfaces
{
    public  interface IUpdater<T> where T : class
    {
        List<string> Process(List<T> changes, long till);
    }
}
