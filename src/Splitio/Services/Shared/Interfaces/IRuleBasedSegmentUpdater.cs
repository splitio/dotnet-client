using Splitio.Domain;
using System.Collections.Generic;

namespace Splitio.Services.Shared.Interfaces
{
    public interface IRuleBasedSegmentUpdater
    {
        List<string> Process(List<RuleBasedSegmentDto> changes, long till);
    }
}
