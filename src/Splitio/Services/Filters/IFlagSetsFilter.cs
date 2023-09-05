using System.Collections.Generic;

namespace Splitio.Services.Filters
{
    public interface IFlagSetsFilter
    {
        bool Match(HashSet<string> sets);
        bool Match(string set);
    }
}
