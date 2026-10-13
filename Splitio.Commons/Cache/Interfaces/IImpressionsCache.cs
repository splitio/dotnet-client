using Splitio.Commons.Domain;
using Splitio.Commons.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Commons.Cache.Interfaces
{
    public interface IImpressionsCache : IPeriodicTask
    {
        int Log(IList<KeyImpression> impressions);
        Task<int> LogAsync(IList<KeyImpression> impressions);
    }
}
