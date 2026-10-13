using Splitio.Commons.Dto;
using System.Collections.Generic;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface ISplitNameValidator
    {
        List<string> SplitNamesAreValid(List<string> splitNames, Commons.Dto.API method);
        ValidatorResult SplitNameIsValid(string splitName, Commons.Dto.API method);
    }
}
