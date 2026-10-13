using Splitio.Commons.Dto;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface ITrafficTypeValidator
    {
        ValidatorResult IsValid(string trafficType, Commons.Dto.API method);
    }
}
