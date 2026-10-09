using Splitio.Commons.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Commons.api.Interfaces
{
    public interface IImpressionsSdkApiClient
    {
        Task SendBulkImpressionsAsync(List<KeyImpression> impressions);
        Task SendBulkImpressionsCountAsync(List<ImpressionsCountModel> impressionsCount);
    }
}
