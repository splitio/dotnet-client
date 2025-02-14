using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsManager
    {
        WrappedKeyImpression Build(ExpectedTreatmentResult treatmentResult, Key key);
        void Track(List<WrappedKeyImpression> impressions);
        Task TrackAsync(List<WrappedKeyImpression> impressions);
    }
}
