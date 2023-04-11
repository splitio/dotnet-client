using Splitio.Domain;
using System.Threading.Tasks;

namespace Splitio.Services.InputValidation.Interfaces
{
    public interface ITrafficTypeValidator
    {
        Task<ValidatorResult> IsValidAsync(string trafficType, string method);
    }
}
