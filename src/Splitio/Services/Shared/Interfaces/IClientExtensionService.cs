using Splitio.Commons.Domain;
using Splitio.Commons.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;
using Splitio.Commons.Shared.Logger;

namespace Splitio.Services.Shared.Interfaces
{
    public interface IClientExtensionService
    {
        List<string> FlagSetsValidations(Commons.Dto.API method, Key key, List<string> flagSets, ISplitLogger logger);
        bool TrackValidations(string key, string trafficType, string eventType, double? value, Dictionary<string, object> properties, out WrappedEvent wrappedEvent);
        bool TreatmentValidations(Commons.Dto.API method, Key key, string featureFlagName, ISplitLogger logger, out string ffNameSanitized);
        List<string> TreatmentsValidations(Commons.Dto.API method, Key key, List<string> features, ISplitLogger logger, out List<TreatmentResult> result);
        void RecordException(Commons.Dto.API method);
        void RecordLatency(Commons.Dto.API method, long latency);
        Task RecordExceptionAsync(Commons.Dto.API method);
        Task RecordLatencyAsync(Commons.Dto.API method, long latency);
        List<TreatmentResult> ReturnControl(List<string> featureFlagNames, string label=null);
    }
}
