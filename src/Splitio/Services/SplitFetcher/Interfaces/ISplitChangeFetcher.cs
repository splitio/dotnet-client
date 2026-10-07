using Splitio.Domain;
using System.Threading.Tasks;
using Splitio.Commons.Dto;


namespace Splitio.Services.SplitFetcher.Interfaces
{
    public interface ISplitChangeFetcher
    {
        Task<TargetingRulesDto> FetchAsync(FetchOptions fetchOptions);
    }
}
