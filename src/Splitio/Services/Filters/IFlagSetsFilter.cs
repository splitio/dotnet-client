using System.Collections.Generic;

namespace Splitio.Services.Filters
{
    public interface IFlagSetsFilter
    {
        bool Intersect(HashSet<string> sets);
        bool Intersect(string set);
    }
}
