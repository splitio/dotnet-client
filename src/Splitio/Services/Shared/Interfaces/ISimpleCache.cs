using System.Collections.Generic;

namespace Splitio.Services.Shared.Interfaces
{
    public interface ISimpleCache<T>
    {
        int AddItems(IList<T> items);
    }
}
