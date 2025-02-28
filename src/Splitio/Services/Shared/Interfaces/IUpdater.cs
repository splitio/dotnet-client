using Splitio.Enums;
using System.Collections.Generic;

namespace Splitio.Services.Shared.Interfaces
{
    public  interface IUpdater<T> where T : class
    {
        Dictionary<SegmentType, List<string>> Process(List<T> changes, long till);
    }
}
