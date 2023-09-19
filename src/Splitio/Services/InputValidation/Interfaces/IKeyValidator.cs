using Splitio.Domain;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface IKeyValidator
    {
        bool IsValid(Key key, Enums.API method);
    }
}
