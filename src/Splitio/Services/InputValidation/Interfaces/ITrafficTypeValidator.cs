using Splitio.Commons.Dto;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface ITrafficTypeValidator
    {
        ValidatorResult IsValid(string trafficType, Enums.API method);
    }
}
