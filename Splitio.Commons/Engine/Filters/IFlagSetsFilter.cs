using System.Collections.Generic;

namespace Splitio.Commons.Engine.Filters
{
    public interface IFlagSetsFilter
    {
        bool Intersect(HashSet<string> sets);
        bool Intersect(string set);
        string GetFlagSets();
    }
}
