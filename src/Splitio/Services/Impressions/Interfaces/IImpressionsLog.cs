using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsLog
    {
        void Start();
        void Stop();
        Task<int> LogAsync(IList<KeyImpression> impressions);
    }
}
