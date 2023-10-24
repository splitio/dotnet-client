using Splitio.Services.Filters;
using System.Collections.Generic;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface IFlagSetsValidator
    {
        HashSet<string> Cleanup(string method, List<string> flagSets);
        HashSet<string> Items(string method, HashSet<string> flagSets, IFlagSetsFilter flagSetsFilter = null);
        bool AreValid(string method, List<string> flagSets, IFlagSetsFilter flagSetsFilter, out HashSet<string> setsToReturn);
    }
}
