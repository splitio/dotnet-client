using Splitio.Commons.Dto;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface IKeyValidator
    {
        bool IsValid(Key key, Enums.API method);
    }
}
