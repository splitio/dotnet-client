using Splitio.Domain;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Generic;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsLog : IPeriodicTask
    {
        int Log(IList<KeyImpression> impressions);
    }
}
