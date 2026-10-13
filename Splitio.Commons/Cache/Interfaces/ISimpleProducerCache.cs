using System.Collections.Generic;

namespace Splitio.Commons.Cache.Interfaces
{
    public interface ISimpleProducerCache<T> : ISimpleCache<T>
    {
        List<T> FetchAllAndClear();
        bool HasReachedMaxSize();
        bool IsEmpty();
    }
}
