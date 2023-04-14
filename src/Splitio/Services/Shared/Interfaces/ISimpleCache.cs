using System.Collections.Generic;

namespace Splitio.Services.Shared.Interfaces
{
    public interface ISimpleCache<T>
    {
        List<T> FetchAllAndClear();
        bool HasReachedMaxSize();
        bool IsEmpty();
    }
}
