using Splitio.Services.Filters;
using System.Collections.Generic;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface IFlagSetsValidator
    {
        HashSet<string> Cleanup(string method, List<string> flagSets);
        HashSet<string> Items(string method, HashSet<string> flagSets, IFlagSetsFilter service = null);
        bool AreValid(string method, List<string> flagSets, IFlagSetsFilter service, out HashSet<string> setsToReturn);
    }
}
