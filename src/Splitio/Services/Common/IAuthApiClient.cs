using Splitio.Commons.Domain;
using System.Threading.Tasks;

namespace Splitio.Services.Common
{
    public interface IAuthApiClient
    {
        Task<AuthenticationResponse> AuthenticateAsync();
    }
}
