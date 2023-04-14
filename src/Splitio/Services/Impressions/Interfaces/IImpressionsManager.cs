using Splitio.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsManager
    {
        KeyImpression BuildImpression(string matchingKey, string feature, string treatment, long time, long? changeNumber, string label, string bucketingKey);
        Task TrackAsync(List<KeyImpression> impressions);
        Task BuildAndTrackAsync(string matchingKey, string feature, string treatment, long time, long? changeNumber, string label, string bucketingKey);
    }
}
