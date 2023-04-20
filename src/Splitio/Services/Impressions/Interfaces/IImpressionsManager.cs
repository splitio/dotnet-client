using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.Impressions.Interfaces
{
    public interface IImpressionsManager
    {
        KeyImpression BuildImpression(string matchingKey, string feature, string treatment, long time, long? changeNumber, string label, string bucketingKey);
        bool Track(List<KeyImpression> impressions);
        bool BuildAndTrack(string matchingKey, string feature, string treatment, long time, long? changeNumber, string label, string bucketingKey);
    }
}
