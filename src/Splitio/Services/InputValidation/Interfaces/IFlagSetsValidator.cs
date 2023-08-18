using System.Collections.Generic;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface IFlagSetsValidator
    {
        HashSet<string> Cleanup(List<string> flagSets);
        HashSet<string> Items(HashSet<string> flagSets);
    }
}
