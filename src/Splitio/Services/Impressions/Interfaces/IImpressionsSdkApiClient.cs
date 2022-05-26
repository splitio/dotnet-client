using Splitio.Domain;
using Splitio.Services.Impressions.Classes;
using System.Collections.Generic;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsSdkApiClient
    {
        void SendBulkImpressions(List<KeyImpression> impressions);
        void SendBulkImpressionsCount(List<ImpressionsCountModel> impressionsCount);
    }
}
