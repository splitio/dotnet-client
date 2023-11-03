using System.Collections.Generic;
using System.Linq;

namespace Splitio.Services.Filters
{
    public class FlagSetsFilter : IFlagSetsFilter
    {
        private readonly HashSet<string> _flagSets;
        private readonly string _flagSetsSorted;
        private readonly bool _shouldFilter;

        public FlagSetsFilter(HashSet<string> sets)
        {
            _shouldFilter = sets.Any();
            _flagSets = sets;

            if (_shouldFilter)
            {
                var setsSorted = sets.OrderBy(x => x).ToList();
                _flagSetsSorted = string.Join(",", setsSorted);
            }
        }

        public bool Intersect(string set)
        {
            if (!_shouldFilter) return true;

            if (string.IsNullOrEmpty(set)) return false;

            return _flagSets
                .Intersect(new HashSet<string> { set })
                .Any();
        }

        public bool Intersect(HashSet<string> sets)
        {
            if (!_shouldFilter) return true;

            if (sets == null) return false;

            return _flagSets
                .Intersect(sets)
                .Any();
        }

        public string GetFlagSets()
        {
            return _flagSetsSorted;
        }
    }
}
