using Splitio.Domain;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsLog : IPeriodicTask
    {
        int Log(IList<KeyImpression> impressions);
        Task<int> LogAsync(IList<KeyImpression> impressions);
    }
}
