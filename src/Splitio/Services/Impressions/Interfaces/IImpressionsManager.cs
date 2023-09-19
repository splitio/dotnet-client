using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsManager
    {
        KeyImpression Build(TreatmentResult treatmentResult, Key key, string feature);
        void Track(List<KeyImpression> impressions);
        Task TrackAsync(List<KeyImpression> impressions);
    }
}
