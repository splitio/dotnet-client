using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface IPropertiesValidator
    {
        PropertiesValidatorResult IsValid(Dictionary<string, object> properties);
    }
}