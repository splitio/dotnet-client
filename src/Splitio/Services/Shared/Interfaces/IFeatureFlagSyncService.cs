using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.Shared.Interfaces
{
    public interface IFeatureFlagSyncService
    {
        List<string> UpdateFeatureFlagsFromChanges(List<Split> changes, long till);
    }
}
