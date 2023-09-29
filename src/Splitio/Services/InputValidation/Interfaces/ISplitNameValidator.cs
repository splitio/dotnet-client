using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface ISplitNameValidator
    {
        List<string> SplitNamesAreValid(List<string> splitNames, Enums.API method);
        ValidatorResult SplitNameIsValid(string splitName, Enums.API method);
    }
}
