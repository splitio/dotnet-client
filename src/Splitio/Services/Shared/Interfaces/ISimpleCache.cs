using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Interfaces
{
    public interface ISimpleCache<T>
    {
        int AddItems(IList<T> items);
        Task<int> AddItemsAsync(IList<T> items);
    }
}
