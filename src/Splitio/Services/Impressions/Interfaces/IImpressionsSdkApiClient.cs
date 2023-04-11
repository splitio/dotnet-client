using Splitio.Domain;
using Splitio.Services.Impressions.Classes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsSdkApiClient
    {
        Task SendBulkImpressionsAsync(List<KeyImpression> impressions);
        Task SendBulkImpressionsCountAsync(List<ImpressionsCountModel> impressionsCount);
    }
}
