using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Filters
{
    public class FlagSetsFilter : IFlagSetsFilter
    {
        private readonly HashSet<string> _flagSets;
        private readonly bool _shouldFilter;

        public FlagSetsFilter(HashSet<string> sets)
        {
            _shouldFilter = sets.Any();
            _flagSets = sets;
        }

        public bool Match(string set)
        {
            if (!_shouldFilter) return true;

            if (string.IsNullOrEmpty(set)) return false;

            return _flagSets.Intersect(new HashSet<string> { set }).Any();
        }

        public bool Match(HashSet<string> sets)
        {
            if (!_shouldFilter) return true;

            if (sets == null) return false;

            return _flagSets.Intersect(sets).Any();
        }
    }
}
