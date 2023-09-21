using Splitio.Domain;
using Splitio.Services.Logger;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Splitio.Services.Shared.Interfaces
{
    public interface IClientExtensionService
    {
        bool TreatmentValidations(Enums.API method, Key key, string featureFlagName, ISplitLogger logger, out string ffNameSanitized);
        List<string> TreatmentsValidations(Enums.API method, Key key, List<string> features, ISplitLogger logger, out List<TreatmentResult> result);
        void RecordException(Enums.API method);
        void RecordLatency(Enums.API method, long latency);
        Task RecordExceptionAsync(Enums.API method);
        Task RecordLatencyAsync(Enums.API method, long latency);
    }
}
