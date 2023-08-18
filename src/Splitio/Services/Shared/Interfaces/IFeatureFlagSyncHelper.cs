using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.Shared.Interfaces
{
    public interface IFeatureFlagSyncHelper
    {
        List<string> UpdateFeatureFlagsFromChanges(List<Split> changes, long till);
    }
}
