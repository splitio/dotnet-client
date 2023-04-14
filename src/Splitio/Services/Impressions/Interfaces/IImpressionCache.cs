using Splitio.Domain;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionCache : ISimpleCache<KeyImpression>
    {
        Task<int> AddItemsAsync(IList<KeyImpression> items);
    }
}
