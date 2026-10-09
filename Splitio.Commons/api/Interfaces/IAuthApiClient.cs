using Splitio.Commons.Domain;
using System.Threading.Tasks;

namespace Splitio.Commons.api.Interfaces
{
    public interface IAuthApiClient
    {
        Task<AuthenticationResponse> AuthenticateAsync();
    }
}
